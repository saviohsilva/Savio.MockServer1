using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Security;
using Savio.MockServer.Services;
using System.Text.Json;

namespace Savio.MockServer.Pages;

public partial class MockEditor
{
    [Parameter]
    public string? Id { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;
    [Inject] private MockAuthConfigService AuthConfigService { get; set; } = default!;
    [Inject] private CertificateService CertificateService { get; set; } = default!;

    private MockEndpoint mock = new()
    {
        Method = "GET",
        StatusCode = 200,
        IsActive = true
    };

    private List<HeaderInput> headersInput = [];
    private List<MockGroup> groups = [];
    private List<MockAuthConfig> authConfigs = [];
    private List<MockCertificate> certificates = [];

    // Auth mode: "" = nenhuma | "issuer" = emite tokens | "protected" = protegido
    private string _authMode = "";
    private MockAuthConfig inlineAuthConfig = new()
    {
        Type = MockAuthType.Bearer,
        GenerateJwtToken = true,
        JwtExpirationMinutes = 60,
        UsernameParamName = "username",
        PasswordParamName = "password",
        UsernameParamLocation = AuthParamLocation.Body,
        PasswordParamLocation = AuthParamLocation.Body,
        CustomTokenReturnLocation = TokenReturnLocation.Body,
        CustomTokenReturnName = "token"
    };

    private bool useJson = true;
    private bool useBinary = false;
    private bool useMultipart = false;
    private bool IsEdit => !string.IsNullOrEmpty(Id);
    private string? saveError;
    private string? currentUserId;
    /// <summary>UserId do usuário-alvo quando um admin cria mock para outro usuário.</summary>
    private string? targetUserId;
    /// <summary>Nome de exibição do usuário-alvo (para o banner informativo).</summary>
    private string? targetUserName;
    private string returnUrl = "/mocks";

    private IBrowserFile? uploadedBinaryFile;
    private string? uploadedBinaryError;
    private string? uploadedMultipartError;

    protected override async Task OnInitializedAsync()
    {
        var uri = new Uri(Navigation.Uri);
        var queryParams = QueryHelpers.ParseQuery(uri.Query);

        await LoadUserContextAsync(queryParams);

        var effectiveUserId = targetUserId ?? currentUserId;
        groups = await MockService.GetAllGroupsAsync(effectiveUserId);
        authConfigs = await AuthConfigService.GetAllAsync(effectiveUserId);
        certificates = await CertificateService.GetAllAsync(effectiveUserId);

        if (queryParams.TryGetValue("returnUrl", out var returnUrlParam) && !string.IsNullOrWhiteSpace(returnUrlParam))
        {
            returnUrl = returnUrlParam.ToString();
        }

        if (queryParams.TryGetValue("from", out var from) && from == "unmocked"
            && queryParams.TryGetValue("id", out var idParam) && int.TryParse(idParam, out int unmockedId))
        {
            await LoadFromUnmockedRequest(unmockedId);
            return;
        }

        if (queryParams.TryGetValue("groupId", out var groupIdParam) && int.TryParse(groupIdParam, out int groupId))
        {
            mock.MockGroupId = groupId;
        }

        if (IsEdit && !string.IsNullOrEmpty(Id))
            await LoadExistingMockAsync();
        else
            headersInput.Add(new HeaderInput { Key = "Content-Type", Value = "application/json" });
    }

    private async Task LoadUserContextAsync(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> queryParams)
    {
        if (AuthState == null) return;

        var authState = await AuthState;
        var user = await UserManager.GetUserAsync(authState.User);
        currentUserId = user?.Id;

        // Admin pode criar mock para outro usuário (via targetUserId na query)
        if (user != null
            && queryParams.TryGetValue("targetUserId", out var targetParam)
            && !string.IsNullOrWhiteSpace(targetParam)
            && targetParam.ToString() != currentUserId
            && await UserManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            targetUserId = targetParam.ToString();
            var targetUser = await UserManager.FindByIdAsync(targetUserId);
            targetUserName = targetUser?.UserName ?? targetUserId;
        }
    }

    private async Task LoadExistingMockAsync()
    {
        var existing = await MockService.GetMockByIdAsync(Id!);
        if (existing == null) return;

        mock = existing;

        useMultipart = !string.IsNullOrWhiteSpace(mock.ResponseMultipartJson);
        useBinary = !useMultipart && (mock.ResponseBinaryBlobId.HasValue || !string.IsNullOrWhiteSpace(mock.ResponseBodyBase64));
        useJson = !useMultipart && !useBinary && !string.IsNullOrEmpty(mock.ResponseBodyJson);

        headersInput = [.. mock.Headers.Select(h => new HeaderInput { Key = h.Key, Value = h.Value })];

        // Restore auth mode
        if (mock.AuthEndpointRole == MockAuthEndpointRole.TokenIssuer && mock.AuthConfigId.HasValue)
        {
            _authMode = "issuer";
            var existingCfg = await AuthConfigService.GetByIdAsync(mock.AuthConfigId.Value);
            if (existingCfg != null)
                inlineAuthConfig = existingCfg;
        }
        else if (mock.AuthEndpointRole == MockAuthEndpointRole.Protected && mock.AuthConfigId.HasValue)
        {
            _authMode = "protected";
        }
    }

    private async Task LoadFromUnmockedRequest(int id)
    {
        var unmockedRequest = await UnmockedRepo.GetByIdAsync(id);
        if (unmockedRequest == null) return;

        mock.Route = await ResolveRouteAliasAsync(unmockedRequest.Route ?? string.Empty);
        mock.Method = unmockedRequest.Method;
        mock.StatusCode = 200;
        mock.IsActive = true;
        mock.Description = $"Mock criado a partir de requisição capturada em {TimezoneService.FormatLocalTime(unmockedRequest.FirstSeenAt, "dd/MM/yyyy HH:mm:ss")}";

        ParseUnmockedRequestHeaders(unmockedRequest.RequestHeadersJson);
        SetDefaultResponseBody(unmockedRequest.RequestBody);

        await UnmockedRepo.MarkAsMockCreatedAsync(id);
    }

    private async Task<string> ResolveRouteAliasAsync(string route)
    {
        var aliasUserId = targetUserId ?? currentUserId;
        if (aliasUserId == null) return route;

        var aliasOwner = await UserManager.FindByIdAsync(aliasUserId);
        if (aliasOwner?.Alias == null) return route;

        var aliasPrefix = $"/{aliasOwner.Alias}";
        if (!route.StartsWith(aliasPrefix, StringComparison.OrdinalIgnoreCase)) return route;

        route = route[aliasPrefix.Length..];
        return route.StartsWith('/') ? route : "/" + route;
    }

    private void ParseUnmockedRequestHeaders(string? requestHeadersJson)
    {
        if (string.IsNullOrEmpty(requestHeadersJson)) return;

        try
        {
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(requestHeadersJson);
            if (headers != null)
            {
                headersInput = [.. headers
                    .Where(h => !h.Key.StartsWith(':') &&
                               !h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                               !h.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                    .Select(h => new HeaderInput { Key = h.Key, Value = h.Value })];
            }
        }
        catch
        {
            headersInput.Add(new HeaderInput { Key = "Content-Type", Value = "application/json" });
        }

        if (headersInput.Count == 0)
            headersInput.Add(new HeaderInput { Key = "Content-Type", Value = "application/json" });
    }

    private void SetDefaultResponseBody(string? requestBody)
    {
        if (!string.IsNullOrEmpty(requestBody))
        {
            mock.ResponseBodyJson = @"{
  ""success"": true,
  ""message"": ""Mock response - ajuste conforme necessário"",
  ""data"": " + requestBody + @"
}";
        }
        else
        {
            mock.ResponseBodyJson = @"{
  ""success"": true,
  ""message"": ""Mock response criado automaticamente""
}";
        }
        useJson = true;
        useBinary = false;
    }

    private void OnAuthModeChanged(string mode)
    {
        _authMode = mode;
        switch (mode)
        {
            case "issuer":
                mock.AuthEndpointRole = MockAuthEndpointRole.TokenIssuer;
                if (!mock.AuthConfigId.HasValue)
                {
                    inlineAuthConfig = new MockAuthConfig
                    {
                        Type = MockAuthType.Bearer,
                        GenerateJwtToken = true,
                        JwtExpirationMinutes = 60,
                        CustomTokenReturnLocation = TokenReturnLocation.Body,
                        CustomTokenReturnName = "token"
                    };
                }
                break;
            case "protected":
                mock.AuthEndpointRole = MockAuthEndpointRole.Protected;
                mock.AuthConfigId = null;
                break;
            default:
                mock.AuthEndpointRole = null;
                mock.AuthConfigId = null;
                break;
        }
    }

    private void OnInlineTypeChanged()
    {
        if (inlineAuthConfig.Type != MockAuthType.Bearer && inlineAuthConfig.Type != MockAuthType.CustomToken)
            inlineAuthConfig.GenerateJwtToken = false;
        if (inlineAuthConfig.Type != MockAuthType.ApiKey)
        {
            inlineAuthConfig.ApiKeyHeader = null;
            inlineAuthConfig.ApiKeyValue = null;
        }

        if (inlineAuthConfig.Type == MockAuthType.CustomToken)
        {
            inlineAuthConfig.GenerateJwtToken = true;
            inlineAuthConfig.CustomTokenReturnName ??= "token";
        }
    }

    private void AddInlineValidationParam()
    {
        inlineAuthConfig.CustomValidationParams.Add(new AuthValidationParam());
    }

    private void RemoveInlineValidationParam(AuthValidationParam param)
    {
        inlineAuthConfig.CustomValidationParams.Remove(param);
    }

    private void AddHeader()
    {
        headersInput.Add(new HeaderInput());
    }

    private void RemoveHeader(HeaderInput header)
    {
        headersInput.Remove(header);
    }

    private void SetResponseType(bool json)
    {
        useMultipart = false;
        useBinary = false;
        useJson = json;

        uploadedBinaryFile = null;
        uploadedBinaryError = null;

        if (json)
        {
            mock.ResponseBodyRaw = string.Empty;
            mock.ResponseMultipartJson = string.Empty;
            mock.ResponseBinaryBlobId = null;
            mock.ResponseBodyBase64 = string.Empty;
            mock.ResponseBodyContentType = string.Empty;
            mock.ResponseBodyFileName = string.Empty;
        }
        else
        {
            mock.ResponseBodyJson = string.Empty;
            mock.ResponseMultipartJson = string.Empty;
            mock.ResponseBinaryBlobId = null;
            mock.ResponseBodyBase64 = string.Empty;
            mock.ResponseBodyContentType = string.Empty;
            mock.ResponseBodyFileName = string.Empty;
        }
    }

    private void SetResponseBinary()
    {
        useMultipart = false;
        useBinary = true;
        useJson = false;

        mock.ResponseMultipartJson = string.Empty;
        mock.ResponseBodyJson = string.Empty;
        mock.ResponseBodyRaw = string.Empty;
        mock.ResponseBodyBase64 = string.Empty;

        if (string.IsNullOrWhiteSpace(mock.ResponseBodyContentType))
        {
            mock.ResponseBodyContentType = "application/octet-stream";
        }
    }

    private void SetResponseMultipart()
    {
        useMultipart = true;
        useBinary = false;
        useJson = false;

        mock.ResponseBodyJson = string.Empty;
        mock.ResponseBodyRaw = string.Empty;
        mock.ResponseBodyBase64 = string.Empty;
        mock.ResponseBodyContentType = string.Empty;
        mock.ResponseBodyFileName = string.Empty;
        mock.ResponseBinaryBlobId = null;

        if (string.IsNullOrWhiteSpace(mock.ResponseMultipartJson))
        {
            mock.ResponseMultipartJson = "{\"subtype\":\"mixed\",\"parts\":[{\"headers\":{\"Content-Disposition\":\"form-data; name=\\\"meta\\\"\"},\"text\":\"ok\"}]}";
        }
    }

    private async Task OnBinaryFileSelected(InputFileChangeEventArgs e)
    {
        uploadedBinaryError = null;
        uploadedBinaryFile = e.File;

        if (uploadedBinaryFile == null)
        {
            return;
        }

        try
        {
            const long maxBytes = 20 * 1024 * 1024;
            await using var stream = uploadedBinaryFile.OpenReadStream(maxBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var blobId = await BinaryStorage.SaveAsync(
                ms.ToArray(),
                uploadedBinaryFile.ContentType,
                uploadedBinaryFile.Name);

            mock.ResponseBinaryBlobId = blobId;
            mock.ResponseBodyFileName = uploadedBinaryFile.Name;
            mock.ResponseBodyContentType = string.IsNullOrWhiteSpace(uploadedBinaryFile.ContentType)
                ? "application/octet-stream"
                : uploadedBinaryFile.ContentType;
        }
        catch (Exception ex)
        {
            uploadedBinaryError = ex.Message;
        }
    }

    private async Task OnMultipartFileSelected(InputFileChangeEventArgs e)
    {
        uploadedMultipartError = null;
        var file = e.File;
        if (file == null)
        {
            return;
        }

        try
        {
            const long maxBytes = 20 * 1024 * 1024;
            await using var stream = file.OpenReadStream(maxBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var blobId = await BinaryStorage.SaveAsync(ms.ToArray(), file.ContentType, file.Name);

            MultipartResponse? multipart;
            try
            {
                multipart = string.IsNullOrWhiteSpace(mock.ResponseMultipartJson)
                    ? null
                    : JsonSerializer.Deserialize<MultipartResponse>(mock.ResponseMultipartJson, _caseInsensitiveOptions);
            }
            catch
            {
                multipart = null;
            }

            multipart ??= new MultipartResponse { Subtype = "mixed" };

            multipart.Parts.Add(new MultipartResponse.Part
            {
                FileName = file.Name,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                BlobId = blobId
            });

            mock.ResponseMultipartJson = JsonSerializer.Serialize(multipart, _indentedOptions);
        }
        catch (Exception ex)
        {
            uploadedMultipartError = ex.Message;
        }
    }

    private async Task Save()
    {
        saveError = null;

        mock.Headers = headersInput
            .Where(h => !string.IsNullOrEmpty(h.Key))
            .ToDictionary(h => h.Key, h => h.Value ?? string.Empty);

        // Quando admin cria para outro usuário, o mock pertence ao usuário-alvo
        var effectiveUserId = targetUserId ?? currentUserId;

        if (!await ApplyAuthModeAsync(effectiveUserId))
            return;

        if (IsEdit)
        {
            var (success, error) = await MockService.UpdateMockAsync(mock, effectiveUserId);
            if (!success)
            {
                saveError = error;
                return;
            }
        }
        else
        {
            var (success, error) = await MockService.AddMockAsync(mock, effectiveUserId);
            if (!success)
            {
                saveError = error;
                return;
            }
        }

        Navigation.NavigateTo(returnUrl);
    }

    /// <summary>Applies auth mode to the mock and returns false if validation failed.</summary>
    private async Task<bool> ApplyAuthModeAsync(string? effectiveUserId)
    {
        if (_authMode == "issuer")
        {
            inlineAuthConfig.CustomValidationParams = inlineAuthConfig.CustomValidationParams
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => new AuthValidationParam
                {
                    Name = p.Name.Trim(),
                    Value = p.Value ?? string.Empty,
                    Location = p.Location
                })
                .ToList();

            if (inlineAuthConfig.Type == MockAuthType.CustomToken && inlineAuthConfig.CustomValidationParams.Count == 0)
            {
                saveError = "Adicione ao menos um parâmetro de validação para Token customizado.";
                return false;
            }

            // Auto-generate name from route+method when not provided
            if (string.IsNullOrWhiteSpace(inlineAuthConfig.Name))
                inlineAuthConfig.Name = $"{mock.Method} {mock.Route}";

            if (mock.AuthConfigId.HasValue)
            {
                // Edit mode: update existing auth config
                inlineAuthConfig.Id = mock.AuthConfigId.Value;
                var (authOk, authErr) = await AuthConfigService.UpdateAsync(inlineAuthConfig);
                if (!authOk)
                {
                    saveError = $"Erro ao atualizar configuração de autenticação: {authErr}";
                    return false;
                }
            }
            else
            {
                // Create new auth config linked to this endpoint
                var (authOk, authErr, newAuthId) = await AuthConfigService.AddAsync(inlineAuthConfig, effectiveUserId);
                if (!authOk)
                {
                    saveError = $"Erro ao salvar configuração de autenticação: {authErr}";
                    return false;
                }
                mock.AuthConfigId = newAuthId;
            }
            mock.AuthEndpointRole = MockAuthEndpointRole.TokenIssuer;
        }
        else if (_authMode == "protected")
        {
            mock.AuthEndpointRole = MockAuthEndpointRole.Protected;
        }
        else
        {
            mock.AuthConfigId = null;
            mock.AuthEndpointRole = null;
        }
        return true;
    }

    private void Cancel()
    {
        Navigation.NavigateTo(returnUrl);
    }

    private static readonly JsonSerializerOptions _caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions _indentedOptions = new() { WriteIndented = true };

    private sealed class HeaderInput
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private sealed class MultipartResponse
    {
        public string Subtype { get; set; } = string.Empty;
        public List<Part> Parts { get; set; } = [];

        public class Part
        {
            public string? FileName { get; set; }
            public string? ContentType { get; set; }
            public long? BlobId { get; set; }
        }
    }
}
