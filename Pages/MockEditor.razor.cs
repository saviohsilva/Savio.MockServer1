using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
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

    private MockEndpoint mock = new()
    {
        Method = "GET",
        StatusCode = 200,
        IsActive = true
    };

    private List<HeaderInput> headersInput = [];
    private List<MockGroup> groups = [];
    private bool useJson = true;
    private bool useBinary = false;
    private bool useMultipart = false;
    private bool IsEdit => !string.IsNullOrEmpty(Id);
    private string? saveError;
    private string? currentUserId;
    private string returnUrl = "/mocks";

    private IBrowserFile? uploadedBinaryFile;
    private string? uploadedBinaryError;
    private string? uploadedMultipartError;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var authState = await AuthState;
            var user = await UserManager.GetUserAsync(authState.User);
            currentUserId = user?.Id;
        }

        groups = await MockService.GetAllGroupsAsync(currentUserId);

        var uri = new Uri(Navigation.Uri);
        var queryParams = QueryHelpers.ParseQuery(uri.Query);

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
        {
            var existing = await MockService.GetMockByIdAsync(Id);
            if (existing != null)
            {
                mock = existing;

                useMultipart = !string.IsNullOrWhiteSpace(mock.ResponseMultipartJson);
                useBinary = !useMultipart && (mock.ResponseBinaryBlobId.HasValue || !string.IsNullOrWhiteSpace(mock.ResponseBodyBase64));
                useJson = !useMultipart && !useBinary && !string.IsNullOrEmpty(mock.ResponseBodyJson);

                headersInput = [.. mock.Headers.Select(h => new HeaderInput { Key = h.Key, Value = h.Value })];
            }
        }
        else
        {
            headersInput.Add(new HeaderInput { Key = "Content-Type", Value = "application/json" });
        }
    }

    private async Task LoadFromUnmockedRequest(int id)
    {
        var unmockedRequest = await UnmockedRepo.GetByIdAsync(id);
        if (unmockedRequest != null)
        {
            var route = unmockedRequest.Route ?? string.Empty;
            if (currentUserId != null)
            {
                var user = await UserManager.FindByIdAsync(currentUserId);
                if (user?.Alias != null)
                {
                    var aliasPrefix = $"/{user.Alias}";
                    if (route.StartsWith(aliasPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        route = route[aliasPrefix.Length..];
                        if (!route.StartsWith('/'))
                            route = "/" + route;
                    }
                }
            }
            mock.Route = route;
            mock.Method = unmockedRequest.Method;
            mock.StatusCode = 200;
            mock.IsActive = true;
            mock.Description = $"Mock criado a partir de requisição capturada em {TimezoneService.FormatLocalTime(unmockedRequest.FirstSeenAt, "dd/MM/yyyy HH:mm:ss")}";

            if (!string.IsNullOrEmpty(unmockedRequest.RequestHeadersJson))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(unmockedRequest.RequestHeadersJson);
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
            }

            if (headersInput.Count == 0)
            {
                headersInput.Add(new HeaderInput { Key = "Content-Type", Value = "application/json" });
            }

            if (!string.IsNullOrEmpty(unmockedRequest.RequestBody))
            {
                mock.ResponseBodyJson = @"{
  ""success"": true,
  ""message"": ""Mock response - ajuste conforme necessário"",
  ""data"": " + unmockedRequest.RequestBody + @"
}";
                useJson = true;
                useBinary = false;
            }
            else
            {
                mock.ResponseBodyJson = @"{
  ""success"": true,
  ""message"": ""Mock response criado automaticamente""
}";
                useJson = true;
                useBinary = false;
            }

            await UnmockedRepo.MarkAsMockCreatedAsync(id);
        }
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

        if (IsEdit)
        {
            var (success, error) = await MockService.UpdateMockAsync(mock, currentUserId);
            if (!success)
            {
                saveError = error;
                return;
            }
        }
        else
        {
            var (success, error) = await MockService.AddMockAsync(mock, currentUserId);
            if (!success)
            {
                saveError = error;
                return;
            }
        }

        Navigation.NavigateTo(returnUrl);
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
