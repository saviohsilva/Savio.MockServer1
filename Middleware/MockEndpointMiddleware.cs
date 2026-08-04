using Savio.MockServer.Services;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Data;
using Savio.MockServer.Helpers;
using Savio.MockServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Savio.MockServer.Middleware;

public class MockEndpointMiddleware(RequestDelegate next, ILogger<MockEndpointMiddleware> logger)
{
    private const string AuthIssuedResponseBodyItemKey = "AuthIssuedResponseBody";
    private readonly RequestDelegate _next = next;
    private readonly ILogger<MockEndpointMiddleware> _logger = logger;
    // Limite de 5MB para captura de texto do response body
    private const int MaxResponseBodySizeForCapture = 5 * 1024 * 1024;

    private sealed record MockServeContext(
        IMockBinaryStorage BinaryStorage,
        MockService MockService,
        IRequestHistoryRepository HistoryRepo,
        IMockRepository MockRepo,
        IMockAuthConfigRepository AuthConfigRepo);

    private sealed record ResponseBodyCapture(
        string Text,
        string? Base64,
        string? ContentType,
        string? FileName);

    internal static readonly string[] InternalPrefixes =
    [
        "/_",
        "/css",
        "/js",
        "/lib",
        "/img",
        "/favicon",
        "/_content",
        "/_blazor",
        "/_framework",
        "/mock",
        "/group",
        "/historico",
        "/unmocked",
        "/mocks",
        "/about",
        "/account",
        "/certificados",
        "/auth-configs"
    ];

    private static bool IsInternalRoute(string path)
    {
        if (path.StartsWith("/account/do-", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path == "/"
            || InternalPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public async Task InvokeAsync(
        HttpContext context,
        MockService mockService,
        IRequestHistoryRepository historyRepo,
        IMockRepository mockRepo,
        IMockBinaryStorage binaryStorage,
        IUnmockedRequestRepository unmockedRepo,
        IMockAuthConfigRepository authConfigRepo,
        MockDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (IsInternalRoute(path))
        {
            await _next(context);
            return;
        }

        try
        {
            await HandleMockRequestAsync(context, mockService, historyRepo, mockRepo, binaryStorage, unmockedRepo, authConfigRepo, dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado no MockEndpointMiddleware para {Method} {Path}", method, path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error", detail = ex.Message }));
            }
        }
    }

    private async Task HandleMockRequestAsync(
        HttpContext context,
        MockService mockService,
        IRequestHistoryRepository historyRepo,
        IMockRepository mockRepo,
        IMockBinaryStorage binaryStorage,
        IUnmockedRequestRepository unmockedRepo,
        IMockAuthConfigRepository authConfigRepo,
        MockDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        var (resolvedAlias, resolvedUserId, mockRoute) = await ResolveAliasAsync(path, dbContext);

        if (resolvedUserId != null && (string.IsNullOrEmpty(mockRoute) || mockRoute == "/"))
        {
            await _next(context);
            return;
        }

        var capturedRequest = await HttpRequestCaptureService.CaptureAsync(context.Request);
        var mocks = await mockService.GetAllMocksAsync(resolvedUserId);
        var mock = FindMock(mocks, mockRoute, method);

        if (mock != null)
        {
            var serveCtx = new MockServeContext(binaryStorage, mockService, historyRepo, mockRepo, authConfigRepo);
            await ServeMockAsync(context, mock, serveCtx, capturedRequest, resolvedUserId);
            return;
        }

        context.Items["MockRoute"] = mockRoute;
        if (resolvedUserId != null)
            context.Items["ResolvedUserId"] = resolvedUserId;

        if (resolvedAlias != null)
        {
            await HandleUnmockedRequestAsync(context, capturedRequest, mockRoute,
                resolvedAlias, resolvedUserId, mocks, unmockedRepo);
            return;
        }

        await _next(context);
    }

    private static MockEndpoint? FindMock(List<MockEndpoint> mocks, string mockRoute, string method)
    {
        return mocks.FirstOrDefault(m =>
                m.Route.Equals(mockRoute, StringComparison.OrdinalIgnoreCase) &&
                m.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
                m.IsActive)
            ?? mocks.FirstOrDefault(m =>
                m.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
                m.IsActive &&
                RouteTemplateHelper.HasRouteParameters(m.Route) &&
                RouteTemplateHelper.MatchesTemplate(m.Route, mockRoute));
    }

    private static async Task<(string? alias, string? userId, string mockRoute)> ResolveAliasAsync(
        string path, MockDbContext dbContext)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 1)
            return (null, null, path);

        var potentialAlias = segments[0];
        var aliasUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Alias == potentialAlias);

        if (aliasUser != null)
            return (potentialAlias, aliasUser.Id, "/" + string.Join("/", segments.Skip(1)));

        if (potentialAlias.EndsWith("api", StringComparison.OrdinalIgnoreCase) && potentialAlias.Length > 3)
        {
            var result = await TryResolveApiAliasAsync(segments, potentialAlias, dbContext);
            if (result.HasValue)
                return result.Value;
        }

        return (null, null, path);
    }

    private static async Task<(string? alias, string? userId, string mockRoute)?> TryResolveApiAliasAsync(
        string[] segments, string potentialAlias, MockDbContext dbContext)
    {
        var aliasPrefix = potentialAlias[..^3];
        var aliasUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Alias == aliasPrefix);

        if (aliasUser == null)
            return null;

        var mockRoute = segments.Length > 1
            ? "/api/" + string.Join("/", segments.Skip(1))
            : "/api";

        return (aliasPrefix, aliasUser.Id, mockRoute);
    }

    private async Task ServeMockAsync(
        HttpContext context,
        MockEndpoint mock,
        MockServeContext serveCtx,
        HttpRequestCaptureService.CapturedRequest capturedRequest,
        string? resolvedUserId)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        _logger.LogInformation("?? [{Method}] {Route} - Status: {StatusCode}",
            mock.Method, mock.Route, mock.StatusCode);

        // ── Certificado de cliente (nível do endpoint) ──────────────────────
        if (mock.RequireClientCertificate)
        {
            var clientCert = context.Connection.ClientCertificate
                ?? await context.Connection.GetClientCertificateAsync();
            if (clientCert == null)
            {
                await WriteUnauthorizedAsync(context, "Este endpoint exige um certificado de cliente (mTLS).");
                return;
            }

            if (!string.IsNullOrWhiteSpace(mock.RequiredClientCertificateThumbprint))
            {
                if (!string.Equals(clientCert.Thumbprint, mock.RequiredClientCertificateThumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteUnauthorizedAsync(context, "Thumbprint do certificado de cliente não confere com o esperado.");
                    return;
                }
            }
        }

        // ── Autenticação ────────────────────────────────────────────────────
        if (mock.AuthConfigId.HasValue && mock.AuthEndpointRole.HasValue)
        {
            var authConfig = await serveCtx.AuthConfigRepo.GetByIdWithCertificateAsync(mock.AuthConfigId.Value);
            if (authConfig != null)
            {
                var authResult = await HandleAuthAsync(context, mock, authConfig, serveCtx);
                if (authResult == AuthHandleResult.Rejected)
                    return;
                if (authResult == AuthHandleResult.TokenIssued)
                {
                    var issuedResponseText = context.Items.TryGetValue(AuthIssuedResponseBodyItemKey, out var raw)
                        ? raw as string
                        : string.Empty;

                    try
                    {
                        await SaveRequestHistoryAsync(
                            context,
                            mock,
                            capturedRequest,
                            resolvedUserId,
                            new ResponseBodyCapture(issuedResponseText ?? string.Empty, null, context.Response.ContentType, null),
                            serveCtx);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao salvar histórico para token emitido em {Method} {Route}", method, path);
                    }

                    return;
                }
            }
        }

        await serveCtx.MockService.RecordCallAsync(mock.Route, mock.Method, resolvedUserId);

        if (mock.DelayMs > 0)
            await Task.Delay(mock.DelayMs);

        foreach (var header in mock.Headers.Where(h => !h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)))
            context.Response.Headers[header.Key] = header.Value;

        context.Response.StatusCode = mock.StatusCode;

        var responseBody = await WriteResponseBodyAsync(context, mock, serveCtx.BinaryStorage);

        try
        {
            await SaveRequestHistoryAsync(context, mock, capturedRequest, resolvedUserId, responseBody, serveCtx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar histórico para {Method} {Route}", method, path);
        }
    }

    private enum AuthHandleResult { Continue, Rejected, TokenIssued }

    private async Task<AuthHandleResult> HandleAuthAsync(
        HttpContext context,
        MockEndpoint mock,
        MockAuthConfigEntity authConfig,
        MockServeContext serveCtx)
    {
        // Valida certificado de cliente (se exigido)
        if (authConfig.RequireCertificate)
        {
            var clientCert = context.Connection.ClientCertificate
                ?? await context.Connection.GetClientCertificateAsync();
            if (clientCert == null)
            {
                await WriteUnauthorizedAsync(context, "Certificado de cliente obrigatório não foi fornecido.");
                return AuthHandleResult.Rejected;
            }

            if (!string.IsNullOrWhiteSpace(authConfig.RequiredCertificate?.Thumbprint))
            {
                if (!string.Equals(clientCert.Thumbprint, authConfig.RequiredCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteUnauthorizedAsync(context, "Thumbprint do certificado de cliente não confere.");
                    return AuthHandleResult.Rejected;
                }
            }
        }

        // Endpoint emissor de token
        if (mock.AuthEndpointRole == MockAuthEndpointRole.TokenIssuer)
            return await HandleTokenIssuerAsync(context, mock, authConfig, serveCtx);

        // Endpoint protegido
        return await HandleProtectedAsync(context, authConfig);
    }

    private async Task<AuthHandleResult> HandleTokenIssuerAsync(
        HttpContext context,
        MockEndpoint mock,
        MockAuthConfigEntity authConfig,
        MockServeContext serveCtx)
    {
        // Extrai credenciais da requisição
        var (username, password) = await ExtractCredentialsAsync(context, authConfig);
        var (customParamsValid, customSubject) = await ValidateCustomValidationParamsAsync(context, authConfig);

        // Valida credenciais
        var credValid = authConfig.Type switch
        {
            MockAuthType.Basic or MockAuthType.Bearer =>
                !string.IsNullOrWhiteSpace(authConfig.Username)
                    ? string.Equals(username, authConfig.Username, StringComparison.Ordinal)
                      && string.Equals(password, authConfig.Password, StringComparison.Ordinal)
                    : true, // sem credenciais configuradas — qualquer um pode obter token
            MockAuthType.ApiKey => ValidateApiKey(context, authConfig),
            MockAuthType.CustomToken => customParamsValid,
            _ => false
        };

        if (!credValid)
        {
            if (authConfig.Type == MockAuthType.CustomToken)
                await WriteUnauthorizedAsync(context, "Parâmetros de autenticação customizados inválidos.");
            else if (string.IsNullOrWhiteSpace(username))
                await WriteUnauthorizedAsync(context, "Nenhuma credencial encontrada na requisição. Envie username/password no body JSON, form ou via Authorization: Basic.");
            else
                await WriteUnauthorizedAsync(context, "Credenciais inválidas.");
            return AuthHandleResult.Rejected;
        }

        // Emite JWT ou resposta estática
        if ((authConfig.Type == MockAuthType.Bearer || authConfig.Type == MockAuthType.CustomToken) && authConfig.GenerateJwtToken)
        {
            var tokenSubject = username
                ?? customSubject
                ?? authConfig.Username
                ?? "mock-user";
            var token = JwtTokenService.GenerateToken(authConfig, tokenSubject);
            var expiresIn = (authConfig.JwtExpirationMinutes > 0 ? authConfig.JwtExpirationMinutes : 60) * 60;

            if (authConfig.Type == MockAuthType.CustomToken)
            {
                var customResponse = await WriteCustomTokenResponseAsync(context, authConfig, token, expiresIn);
                context.Items[AuthIssuedResponseBodyItemKey] = customResponse;
            }
            else
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json; charset=utf-8";
                var standardTokenResponse = JsonSerializer.Serialize(new
                {
                    access_token = token,
                    token_type = "Bearer",
                    expires_in = expiresIn
                });
                await context.Response.WriteAsync(standardTokenResponse, Encoding.UTF8);
                context.Items[AuthIssuedResponseBodyItemKey] = standardTokenResponse;
            }

            await serveCtx.MockService.RecordCallAsync(mock.Route, mock.Method, null);
            return AuthHandleResult.TokenIssued;
        }

        // Para outros tipos, serve a resposta configurada normalmente
        return AuthHandleResult.Continue;
    }

    private static async Task<AuthHandleResult> HandleProtectedAsync(
        HttpContext context,
        MockAuthConfigEntity authConfig)
    {
        var isValid = authConfig.Type switch
        {
            MockAuthType.Bearer => ValidateBearerToken(context, authConfig),
            MockAuthType.Basic => ValidateBasicAuth(context, authConfig),
            MockAuthType.ApiKey => ValidateApiKey(context, authConfig),
            MockAuthType.CustomToken => await ValidateCustomTokenAsync(context, authConfig),
            _ => false
        };

        if (!isValid)
        {
            await WriteUnauthorizedAsync(context, "Autenticação inválida ou ausente.");
            return AuthHandleResult.Rejected;
        }

        return AuthHandleResult.Continue;
    }

    private static bool ValidateBearerToken(HttpContext context, MockAuthConfigEntity authConfig)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            return false;

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = headerValue["Bearer ".Length..].Trim();

        if (authConfig.GenerateJwtToken)
            return JwtTokenService.ValidateToken(authConfig, token);

        // Sem JWT configurado: aceita qualquer Bearer não-vazio
        return !string.IsNullOrWhiteSpace(token);
    }

    private static bool ValidateBasicAuth(HttpContext context, MockAuthConfigEntity authConfig)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            return false;

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue["Basic ".Length..].Trim()));
            var sep = decoded.IndexOf(':');
            if (sep < 0) return false;

            var user = decoded[..sep];
            var pass = decoded[(sep + 1)..];

            return string.Equals(user, authConfig.Username, StringComparison.Ordinal)
                && string.Equals(pass, authConfig.Password, StringComparison.Ordinal);
        }
        catch { return false; }
    }

    private static bool ValidateApiKey(HttpContext context, MockAuthConfigEntity authConfig)
    {
        if (string.IsNullOrWhiteSpace(authConfig.ApiKeyHeader) || string.IsNullOrWhiteSpace(authConfig.ApiKeyValue))
            return false;

        if (!context.Request.Headers.TryGetValue(authConfig.ApiKeyHeader, out var headerValue))
            return false;

        return string.Equals(headerValue.ToString(), authConfig.ApiKeyValue, StringComparison.Ordinal);
    }

    private static async Task<(string? username, string? password)> ExtractCredentialsAsync(
        HttpContext context, MockAuthConfigEntity authConfig)
    {
        var configuredUsernameName = string.IsNullOrWhiteSpace(authConfig.UsernameParamName)
            ? "username"
            : authConfig.UsernameParamName.Trim();
        var configuredPasswordName = string.IsNullOrWhiteSpace(authConfig.PasswordParamName)
            ? "password"
            : authConfig.PasswordParamName.Trim();

        // 1. Authorization: Basic base64(user:pass) — aceito para qualquer tipo
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var val = authHeader.ToString();
            if (val.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(val["Basic ".Length..].Trim()));
                    var sep = decoded.IndexOf(':');
                    if (sep >= 0)
                        return (decoded[..sep], decoded[(sep + 1)..]);
                }
                catch { }
            }
        }

        // 1.1 Busca customizável (local + nome configurados)
        var configuredUser = await TryReadConfiguredParamAsync(context, configuredUsernameName, authConfig.UsernameParamLocation);
        var configuredPass = await TryReadConfiguredParamAsync(context, configuredPasswordName, authConfig.PasswordParamLocation);
        if (!string.IsNullOrWhiteSpace(configuredUser))
        {
            return (configuredUser, configuredPass);
        }

        // 2. Form body: username/password ou client_id/client_secret
        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = await context.Request.ReadFormAsync();
                var user = form["username"].FirstOrDefault() ?? form["client_id"].FirstOrDefault();
                var pass = form["password"].FirstOrDefault() ?? form["client_secret"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(user))
                    return (user, pass);
            }
            catch { }
        }

        // 3. JSON body: {"username":"...","password":"..."} ou {"client_id":"...","client_secret":"..."}
        //    Tenta mesmo sem Content-Type explícito (fallback robusto)
        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith('{'))
            {
                var doc = JsonDocument.Parse(body);
                doc.RootElement.TryGetProperty("username", out var uProp);
                doc.RootElement.TryGetProperty("client_id", out var cidProp);
                doc.RootElement.TryGetProperty("password", out var pProp);
                doc.RootElement.TryGetProperty("client_secret", out var csProp);

                var user = uProp.ValueKind == JsonValueKind.String ? uProp.GetString()
                         : cidProp.ValueKind == JsonValueKind.String ? cidProp.GetString() : null;

                // Aceita password como string OU número (ex: "password": 123 ou "password": "123")
                var pass = pProp.ValueKind == JsonValueKind.String ? pProp.GetString()
                         : pProp.ValueKind == JsonValueKind.Number ? pProp.GetRawText()
                         : csProp.ValueKind == JsonValueKind.String ? csProp.GetString()
                         : csProp.ValueKind == JsonValueKind.Number ? csProp.GetRawText()
                         : null;

                if (!string.IsNullOrWhiteSpace(user))
                    return (user, pass);
            }
        }
        catch { }

        return (null, null);
    }

    private static async Task<(bool isValid, string? principal)> ValidateCustomValidationParamsAsync(
        HttpContext context,
        MockAuthConfigEntity authConfig)
    {
        if (string.IsNullOrWhiteSpace(authConfig.CustomValidationParamsJson))
        {
            return (false, null);
        }

        List<AuthValidationParamConfig>? configured;
        try
        {
            configured = JsonSerializer.Deserialize<List<AuthValidationParamConfig>>(authConfig.CustomValidationParamsJson);
        }
        catch
        {
            return (false, null);
        }

        if (configured == null || configured.Count == 0)
        {
            return (false, null);
        }

        string? principal = null;
        var hasAnyNamedParam = false;

        foreach (var param in configured)
        {
            if (string.IsNullOrWhiteSpace(param.Name))
            {
                continue;
            }

            hasAnyNamedParam = true;
            var actual = await TryReadConfiguredParamAsync(context, param.Name, param.Location);
            if (!string.Equals(actual, param.Value ?? string.Empty, StringComparison.Ordinal))
            {
                return (false, null);
            }

            principal ??= actual;
        }

        return (hasAnyNamedParam, principal);
    }

    private static async Task<bool> ValidateCustomTokenAsync(HttpContext context, MockAuthConfigEntity authConfig)
    {
        var tokenName = string.IsNullOrWhiteSpace(authConfig.CustomTokenReturnName)
            ? "token"
            : authConfig.CustomTokenReturnName.Trim();

        string? incomingToken = null;
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                incomingToken = headerValue["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(incomingToken))
        {
            if (authConfig.CustomTokenReturnLocation == TokenReturnLocation.Header)
            {
                if (context.Request.Headers.TryGetValue(tokenName, out var customHeader))
                {
                    incomingToken = customHeader.FirstOrDefault();
                }
            }
            else
            {
                incomingToken = await TryReadConfiguredParamAsync(context, tokenName, AuthParamLocation.Body);
            }
        }

        var normalized = StripCustomTokenDecorators(incomingToken, authConfig.CustomTokenPrefix, authConfig.CustomTokenSuffix);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (authConfig.GenerateJwtToken)
        {
            return JwtTokenService.ValidateToken(authConfig, normalized);
        }

        return true;
    }

    private static string? StripCustomTokenDecorators(string? token, string? prefix, string? suffix)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var value = token.Trim();

        if (!string.IsNullOrEmpty(prefix))
        {
            if (!value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }
            value = value[prefix.Length..];
        }

        if (!string.IsNullOrEmpty(suffix))
        {
            if (!value.EndsWith(suffix, StringComparison.Ordinal))
            {
                return null;
            }
            value = value[..^suffix.Length];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static async Task<string> WriteCustomTokenResponseAsync(
        HttpContext context,
        MockAuthConfigEntity authConfig,
        string token,
        int expiresIn)
    {
        var tokenName = string.IsNullOrWhiteSpace(authConfig.CustomTokenReturnName)
            ? "token"
            : authConfig.CustomTokenReturnName.Trim();

        var composedToken = string.Concat(
            authConfig.CustomTokenPrefix ?? string.Empty,
            token,
            authConfig.CustomTokenSuffix ?? string.Empty);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json; charset=utf-8";

        if (authConfig.CustomTokenReturnLocation == TokenReturnLocation.Header)
        {
            context.Response.Headers[tokenName] = composedToken;
            var responseBody = JsonSerializer.Serialize(new
            {
                token_type = "Custom",
                expires_in = expiresIn
            });
            await context.Response.WriteAsync(responseBody, Encoding.UTF8);
            return responseBody;
        }

        var bodyJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [tokenName] = composedToken,
            ["token_type"] = "Custom",
            ["expires_in"] = expiresIn
        });
        await context.Response.WriteAsync(bodyJson, Encoding.UTF8);
        return bodyJson;
    }

    private static async Task<string?> TryReadConfiguredParamAsync(HttpContext context, string paramName, AuthParamLocation location)
    {
        if (string.IsNullOrWhiteSpace(paramName))
        {
            return null;
        }

        switch (location)
        {
            case AuthParamLocation.QueryString:
                if (context.Request.Query.TryGetValue(paramName, out var queryValue))
                {
                    return queryValue.FirstOrDefault();
                }
                return null;

            case AuthParamLocation.Header:
                if (context.Request.Headers.TryGetValue(paramName, out var headerValue))
                {
                    return headerValue.FirstOrDefault();
                }
                return null;

            case AuthParamLocation.Body:
            default:
                if (context.Request.HasFormContentType)
                {
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        if (form.TryGetValue(paramName, out var formValue))
                        {
                            return formValue.FirstOrDefault();
                        }
                    }
                    catch
                    {
                        // Ignora erro de form e tenta JSON
                    }
                }

                try
                {
                    context.Request.EnableBuffering();
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith('{'))
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty(paramName, out var jsonProp))
                        {
                            return jsonProp.ValueKind switch
                            {
                                JsonValueKind.String => jsonProp.GetString(),
                                JsonValueKind.Number => jsonProp.GetRawText(),
                                JsonValueKind.True => "true",
                                JsonValueKind.False => "false",
                                _ => jsonProp.GetRawText()
                            };
                        }
                    }
                }
                catch
                {
                    // Ignora erro de JSON
                }

                return null;
        }
    }

    private sealed class AuthValidationParamConfig
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
        public AuthParamLocation Location { get; set; } = AuthParamLocation.Body;
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers["WWW-Authenticate"] = "Bearer";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { error = "unauthorized", message }),
            Encoding.UTF8);
    }

    private static async Task<ResponseBodyCapture> WriteResponseBodyAsync(
        HttpContext context, MockEndpoint mock, IMockBinaryStorage binaryStorage)
    {
        if (!string.IsNullOrWhiteSpace(mock.ResponseMultipartJson))
            return await WriteMultipartResponseAsync(context, mock, binaryStorage);

        if (mock.ResponseBinaryBlobId.HasValue)
            return await WriteBlobResponseAsync(context, mock, binaryStorage);

        if (!string.IsNullOrWhiteSpace(mock.ResponseBodyBase64))
            return await WriteBase64ResponseAsync(context, mock);

        if (!string.IsNullOrEmpty(mock.ResponseBodyRaw))
        {
            context.Response.ContentType = mock.Headers.GetValueOrDefault("Content-Type") ?? "application/json";
            await context.Response.WriteAsync(mock.ResponseBodyRaw, Encoding.UTF8);
            return new ResponseBodyCapture(mock.ResponseBodyRaw, null, null, null);
        }

        if (!string.IsNullOrEmpty(mock.ResponseBodyJson))
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(mock.ResponseBodyJson, Encoding.UTF8);
            return new ResponseBodyCapture(mock.ResponseBodyJson, null, null, null);
        }

        return new ResponseBodyCapture(string.Empty, null, null, null);
    }

    private static async Task<ResponseBodyCapture> WriteMultipartResponseAsync(
        HttpContext context, MockEndpoint mock, IMockBinaryStorage binaryStorage)
    {
        var wroteMultipart = await MultipartResponseWriter.TryWriteMultipartAsync(
            context, mock.ResponseMultipartJson!, binaryStorage, context.RequestAborted);
        return wroteMultipart
            ? new ResponseBodyCapture(mock.ResponseMultipartJson!, null, context.Response.ContentType, null)
            : new ResponseBodyCapture(string.Empty, null, null, null);
    }

    private static async Task<ResponseBodyCapture> WriteBlobResponseAsync(
        HttpContext context, MockEndpoint mock, IMockBinaryStorage binaryStorage)
    {
        var blob = await binaryStorage.GetAsync(mock.ResponseBinaryBlobId!.Value, context.RequestAborted);
        if (blob == null)
            return new ResponseBodyCapture(string.Empty, null, null, null);

        var rawContentType = string.IsNullOrWhiteSpace(blob.Value.contentType)
            ? "application/octet-stream"
            : blob.Value.contentType;

        var assessment = BinaryContentInspector.AssessForInlinePreview(blob.Value.bytes, rawContentType);

        context.Response.ContentType = assessment.EffectiveContentType;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ApplySafeContentDisposition(context.Response, blob.Value.fileName, assessment.CanInlinePreview);

        await context.Response.Body.WriteAsync(blob.Value.bytes, context.RequestAborted);
        return new ResponseBodyCapture(string.Empty, null, assessment.EffectiveContentType, blob.Value.fileName);
    }

    private static async Task<ResponseBodyCapture> WriteBase64ResponseAsync(
        HttpContext context, MockEndpoint mock)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(mock.ResponseBodyBase64!);
        }
        catch
        {
            bytes = [];
        }

        var contentType = string.IsNullOrWhiteSpace(mock.ResponseBodyContentType)
            ? (mock.Headers.GetValueOrDefault("Content-Type") ?? "application/octet-stream")
            : mock.ResponseBodyContentType;

        var assessment = BinaryContentInspector.AssessForInlinePreview(bytes, contentType);

        context.Response.ContentType = assessment.EffectiveContentType;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ApplySafeContentDisposition(context.Response, mock.ResponseBodyFileName, assessment.CanInlinePreview);

        await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
        return new ResponseBodyCapture(string.Empty, mock.ResponseBodyBase64, assessment.EffectiveContentType, mock.ResponseBodyFileName);
    }

    private static void ApplySafeContentDisposition(HttpResponse response, string? fileName, bool canInlinePreview)
    {
        var dispositionType = canInlinePreview ? "inline" : "attachment";

        if (string.IsNullOrWhiteSpace(fileName))
        {
            response.Headers.ContentDisposition = dispositionType;
            return;
        }

        var safeFileName = fileName.Replace("\"", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        response.Headers.ContentDisposition = $"{dispositionType}; filename=\"{safeFileName}\"";
    }

    private async Task SaveRequestHistoryAsync(
        HttpContext context,
        MockEndpoint mock,
        HttpRequestCaptureService.CapturedRequest capturedRequest,
        string? resolvedUserId,
        ResponseBodyCapture responseBody,
        MockServeContext serveCtx)
    {
        var mockEntity = await serveCtx.MockRepo.GetActiveByRouteAndMethodAsync(mock.Route, mock.Method, null, resolvedUserId);
        if (mockEntity == null)
            return;

        var (capturedBody, blobId) =
            await LimitResponseBodySizeAsync(responseBody.Text, mockEntity.Id, context, serveCtx.BinaryStorage);

        await serveCtx.HistoryRepo.AddAsync(new RequestHistoryEntity
        {
            MockEndpointId = mockEntity.Id,
            Method = context.Request.Method,
            Route = context.Request.Path,
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            RequestHeadersJson = JsonSerializer.Serialize(
                context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
            RequestBody = capturedRequest.TextBody,
            RequestFormJson = capturedRequest.FormJson,
            RequestBodyBase64 = capturedRequest.BodyBase64,
            RequestBodyContentType = capturedRequest.BodyContentType,
            RequestBodyFileName = capturedRequest.BodyFileName,
            ResponseStatusCode = context.Response.StatusCode,
            ResponseHeadersJson = JsonSerializer.Serialize(
                context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
            ResponseBody = capturedBody,
            ResponseBodyBase64 = responseBody.Base64,
            ResponseBodyContentType = responseBody.ContentType,
            ResponseBodyFileName = responseBody.FileName,
            ResponseBinaryBlobId = blobId,
            DelayMs = mock.DelayMs,
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            RequestedAt = DateTime.UtcNow
        });
    }

    private async Task<(string capturedBody, int? blobId)> LimitResponseBodySizeAsync(
        string responseBodyText, int mockEntityId, HttpContext context, IMockBinaryStorage binaryStorage)
    {
        if (string.IsNullOrEmpty(responseBodyText))
            return (responseBodyText, null);

        var responseBodySize = Encoding.UTF8.GetByteCount(responseBodyText);
        if (responseBodySize <= MaxResponseBodySizeForCapture)
            return (responseBodyText, null);

        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        _logger.LogInformation("Response body grande ({Size} bytes) para {Method} {Route}, salvando como blob",
            responseBodySize, method, path);

        try
        {
            var responseBytes = Encoding.UTF8.GetBytes(responseBodyText);
            var blobId = await binaryStorage.SaveAsync(
                responseBytes,
                context.Response.ContentType ?? "application/octet-stream",
                $"response_{mockEntityId}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
            return ($"[Response grande: {responseBodySize} bytes - armazenado como BlobId={blobId}]", blobId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao salvar response body como blob, truncando captura");
            return ($"[Response muito grande: {responseBodySize} bytes - não capturado completamente]", null);
        }
    }

    private async Task HandleUnmockedRequestAsync(
        HttpContext context,
        HttpRequestCaptureService.CapturedRequest capturedRequest,
        string mockRoute,
        string resolvedAlias,
        string? resolvedUserId,
        List<MockEndpoint> mocks,
        IUnmockedRequestRepository unmockedRepo)
    {
        var method = context.Request.Method;

        _logger.LogWarning(
            "Mock não encontrado para [{Method}] {Route} (alias={Alias}, userId={UserId}). Mocks disponíveis: {Count}",
            method, mockRoute, resolvedAlias, resolvedUserId, mocks.Count);

        try
        {
            await unmockedRepo.AddOrUpdateAsync(new UnmockedRequestEntity
            {
                Method = method,
                Route = mockRoute,
                RequestHeadersJson = JsonSerializer.Serialize(
                    context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
                RequestBody = capturedRequest.TextBody,
                RequestFormJson = capturedRequest.FormJson,
                RequestBodyBase64 = capturedRequest.BodyBase64,
                RequestBodyContentType = capturedRequest.BodyContentType,
                RequestBodyFileName = capturedRequest.BodyFileName,
                LastClientIp = context.Connection.RemoteIpAddress?.ToString(),
                LastSeenAt = DateTime.UtcNow,
                UserId = resolvedUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar unmocked request {Method} {Route}", method, mockRoute);
        }

        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new
            {
                error = "Mock não encontrado",
                route = mockRoute,
                method,
                alias = resolvedAlias,
                hint = $"Verifique se existe um mock ativo com rota '{mockRoute}' e método '{method}'.",
                availableMocks = mocks
                    .Where(m => m.IsActive)
                    .Select(m => new { m.Route, m.Method })
                    .ToArray()
            }), Encoding.UTF8);
    }
}

