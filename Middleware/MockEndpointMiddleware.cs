using Savio.MockServer.Services;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Data;
using Savio.MockServer.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Savio.MockServer.Middleware;

public class MockEndpointMiddleware(RequestDelegate next, ILogger<MockEndpointMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<MockEndpointMiddleware> _logger = logger;
    // Limite de 5MB para captura de texto do response body
    private const int MaxResponseBodySizeForCapture = 5 * 1024 * 1024;

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
        "/account"
    ];

    private static bool IsInternalRoute(string path)
    {
        if (path == "/")
            return true;

        foreach (var prefix in InternalPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public async Task InvokeAsync(
        HttpContext context,
        MockService mockService,
        IRequestHistoryRepository historyRepo,
        IMockRepository mockRepo,
        IMockBinaryStorage binaryStorage,
        IUnmockedRequestRepository unmockedRepo,
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
            await HandleMockRequestAsync(context, mockService, historyRepo, mockRepo, binaryStorage, unmockedRepo, dbContext, path, method);
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
        MockDbContext dbContext,
        string path,
        string method)
    {
        // Tentar resolver rota com alias: /{alias}/{rota}
        string? resolvedAlias = null;
        string? resolvedUserId = null;
        string mockRoute = path;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 1)
        {
            var potentialAlias = segments[0];
            var aliasUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Alias == potentialAlias);
            if (aliasUser != null)
            {
                resolvedAlias = potentialAlias;
                resolvedUserId = aliasUser.Id;
                mockRoute = "/" + string.Join("/", segments.Skip(1));
                if (string.IsNullOrEmpty(mockRoute) || mockRoute == "/")
                {
                    await _next(context);
                    return;
                }
            }
        }

        var capturedRequest = await HttpRequestCaptureService.CaptureAsync(context.Request);

        var mocks = await mockService.GetAllMocksAsync(resolvedUserId);
        var mock = mocks.FirstOrDefault(m =>
            m.Route.Equals(mockRoute, StringComparison.OrdinalIgnoreCase) &&
            m.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
            m.IsActive)
            ?? mocks.FirstOrDefault(m =>
                m.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
                m.IsActive &&
                RouteTemplateHelper.HasRouteParameters(m.Route) &&
                RouteTemplateHelper.MatchesTemplate(m.Route, mockRoute));

        if (mock != null)
        {
            _logger.LogInformation("?? [{Method}] {Route} - Status: {StatusCode}",
                mock.Method, mock.Route, mock.StatusCode);

            await mockService.RecordCallAsync(mock.Route, mock.Method, resolvedUserId);

            if (mock.DelayMs > 0)
            {
                await Task.Delay(mock.DelayMs);
            }

            foreach (var header in mock.Headers.Where(h => !h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.Headers[header.Key] = header.Value;
            }

            context.Response.StatusCode = mock.StatusCode;

            string responseBodyText = string.Empty;
            string? responseBodyBase64 = null;
            string? responseBodyContentType = null;
            string? responseBodyFileName = null;

            // 1) Multipart > 2) Binário (blob) > 3) Binário (legado base64) > 4) Raw > 5) JSON
            if (!string.IsNullOrWhiteSpace(mock.ResponseMultipartJson))
            {
                var wroteMultipart = await MultipartResponseWriter.TryWriteMultipartAsync(context, mock.ResponseMultipartJson, binaryStorage, context.RequestAborted);
                if (wroteMultipart)
                {
                    responseBodyText = mock.ResponseMultipartJson; // registra config
                    responseBodyContentType = context.Response.ContentType;
                }
            }
            else if (mock.ResponseBinaryBlobId.HasValue)
            {
                var blob = await binaryStorage.GetAsync(mock.ResponseBinaryBlobId.Value, context.RequestAborted);
                if (blob != null)
                {
                    responseBodyContentType = blob.Value.contentType;
                    responseBodyFileName = blob.Value.fileName;

                    context.Response.ContentType = responseBodyContentType;
                    if (!string.IsNullOrWhiteSpace(responseBodyFileName))
                    {
                        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{responseBodyFileName}\"";
                    }

                    await context.Response.Body.WriteAsync(blob.Value.bytes, context.RequestAborted);
                }
            }
            else if (!string.IsNullOrWhiteSpace(mock.ResponseBodyBase64))
            {
                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(mock.ResponseBodyBase64);
                }
                catch
                {
                    bytes = [];
                }

                responseBodyBase64 = mock.ResponseBodyBase64;
                responseBodyContentType = string.IsNullOrWhiteSpace(mock.ResponseBodyContentType)
                    ? (mock.Headers.GetValueOrDefault("Content-Type") ?? "application/octet-stream")
                    : mock.ResponseBodyContentType;
                responseBodyFileName = mock.ResponseBodyFileName;

                context.Response.ContentType = responseBodyContentType;
                if (!string.IsNullOrWhiteSpace(responseBodyFileName))
                {
                    context.Response.Headers.ContentDisposition = $"attachment; filename=\"{responseBodyFileName}\"";
                }

                await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
            }
            else if (!string.IsNullOrEmpty(mock.ResponseBodyRaw))
            {
                var contentType = mock.Headers.GetValueOrDefault("Content-Type") ?? "application/json";
                context.Response.ContentType = contentType;
                responseBodyText = mock.ResponseBodyRaw;
                await context.Response.WriteAsync(responseBodyText, Encoding.UTF8);
            }
            else if (!string.IsNullOrEmpty(mock.ResponseBodyJson))
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                responseBodyText = mock.ResponseBodyJson;
                await context.Response.WriteAsync(responseBodyText, Encoding.UTF8);
            }

            try
            {
                var mockEntity = await mockRepo.GetActiveByRouteAndMethodAsync(mock.Route, mock.Method, null, resolvedUserId);

                if (mockEntity != null)
                {
                    // Limitar tamanho do response body capturado
                    string capturedResponseBody = responseBodyText;
                    int? responseBlobId = null;
                    
                    if (!string.IsNullOrEmpty(responseBodyText))
                    {
                        var responseBodySize = Encoding.UTF8.GetByteCount(responseBodyText);
                        
                        if (responseBodySize > MaxResponseBodySizeForCapture)
                        {
                            _logger.LogInformation("Response body grande ({Size} bytes) para {Method} {Route}, salvando como blob", 
                                responseBodySize, method, path);
                            
                            try
                            {
                                // Salvar response grande como blob
                                var responseBytes = Encoding.UTF8.GetBytes(responseBodyText);
                                responseBlobId = await binaryStorage.SaveAsync(
                                    responseBytes,
                                    context.Response.ContentType ?? "application/octet-stream",
                                    $"response_{mockEntity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
                                
                                // Limpar o texto para não duplicar no banco
                                capturedResponseBody = $"[Response grande: {responseBodySize} bytes - armazenado como BlobId={responseBlobId}]";
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Erro ao salvar response body como blob, truncando captura");
                                capturedResponseBody = $"[Response muito grande: {responseBodySize} bytes - não capturado completamente]";
                            }
                        }
                    }

                    var history = new RequestHistoryEntity
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
                        ResponseBody = capturedResponseBody,
                        ResponseBodyBase64 = responseBodyBase64,
                        ResponseBodyContentType = responseBodyContentType,
                        ResponseBodyFileName = responseBodyFileName,
                        ResponseBinaryBlobId = responseBlobId,
                        DelayMs = mock.DelayMs,
                        ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                        RequestedAt = DateTime.UtcNow
                    };

                    await historyRepo.AddAsync(history);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar histórico para {Method} {Route}", method, path);
            }

            return;
        }

        // Compartilhar dados da resolução do alias com o RequestHistoryMiddleware
        context.Items["MockRoute"] = mockRoute;
        if (resolvedUserId != null)
            context.Items["ResolvedUserId"] = resolvedUserId;

        // Se o alias foi resolvido mas nenhum mock bateu, retornar 404 JSON
        // em vez de cair no Blazor (que confunde o cliente)
        if (resolvedAlias != null)
        {
            _logger.LogWarning(
                "Mock não encontrado para [{Method}] {Route} (alias={Alias}, userId={UserId}). Mocks disponíveis: {Count}",
                method, mockRoute, resolvedAlias, resolvedUserId, mocks.Count);

            // Registrar como requisição não mockada (rota sem prefixo de alias)
            try
            {
                var unmockedRequest = new UnmockedRequestEntity
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
                    LastSeenAt = DateTime.UtcNow
                };
                await unmockedRepo.AddOrUpdateAsync(unmockedRequest);
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
            return;
        }

        await _next(context);
    }
}

