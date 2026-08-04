using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Net.Http.Headers;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Helpers;
using Savio.MockServer.Services;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Savio.MockServer.Pages;

public partial class HistoricoDetalhes
{
    private const string EmptyLabel = "(vazio)";
    private const string DownloadBase64File = "downloadBase64File";
    [CascadingParameter]
    public IModalService Modal { get; set; } = default!;

    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    private bool isLoading = true;
    private bool requestBodyWrap = true;
    private bool responseBodyWrap = true;
    private RequestHistoryEntity? history;
    private Dictionary<string, string>? requestHeaders;
    private Dictionary<string, string>? responseHeaders;
    private Dictionary<string, string>? queryParams;
    private BinaryPreviewState? requestBase64Preview = null;
    private List<MultipartRequestFile> requestMultipartFiles = [];
    private byte[]? responseBlobContent = null;
    private BinaryPreviewState? responseBlobPreview = null;
    private BinaryPreviewState? responseBase64Preview = null;
    private bool loadingBlobContent = false;

    private sealed record BinaryPreviewState(
        bool CanPreview,
        bool IsPdf,
        string MimeType,
        string? DataUrl,
        long SizeBytes,
        string? BlockReason);

    private sealed record MultipartRequestFile(
        string FieldName,
        string FileName,
        string ContentType,
        byte[] Bytes,
        BinaryPreviewState Preview);

    private bool HasMultipartRequestFiles => requestMultipartFiles.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        history = await HistoryRepo.GetByIdAsync(Id);

        if (history != null)
        {
            try
            {
                if (!string.IsNullOrEmpty(history.RequestHeadersJson))
                {
                    requestHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(history.RequestHeadersJson);
                }
                if (!string.IsNullOrEmpty(history.ResponseHeadersJson))
                {
                    responseHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(history.ResponseHeadersJson);
                }
                if (!string.IsNullOrEmpty(history.QueryString))
                {
                    queryParams = ParseQueryString(history.QueryString);
                }

                if (!string.IsNullOrWhiteSpace(history.RequestBodyBase64))
                {
                    requestMultipartFiles = await ExtractMultipartRequestFilesAsync(history.RequestBodyBase64, history.RequestBodyContentType);
                    if (!HasMultipartRequestFiles)
                    {
                        requestBase64Preview = BuildPreviewFromBase64(history.RequestBodyBase64, history.RequestBodyContentType);
                    }
                }

                if (!string.IsNullOrWhiteSpace(history.ResponseBodyBase64))
                {
                    responseBase64Preview = BuildPreviewFromBase64(history.ResponseBodyBase64, history.ResponseBodyContentType);
                }
            }
            catch
            {
                // Headers/querystring deserialization failures are non-critical; ignore
            }
        }

        isLoading = false;
    }

    private async Task Excluir()
    {
        if (history is null)
        {
            return;
        }

        var confirmed = await ConfirmActionAsync(
            "Confirmar Exclusão",
            "Confirma a exclusão deste item do histórico?",
            "bi-trash",
            "danger");
        if (!confirmed)
        {
            return;
        }

        await HistoryRepo.DeleteByIdAsync(Id);
        Navigation.NavigateTo("/historico", forceLoad: true);
    }

    private string GetResponseFileName()
    {
        var baseName = !string.IsNullOrWhiteSpace(history?.ResponseBodyFileName)
            ? history.ResponseBodyFileName
            : BuildFallbackFileName("response", history?.ResponseBodyContentType);

        return AppendRequestCodeToFileName(baseName);
    }

    private string AppendRequestCodeToFileName(string originalFileName)
    {
        var sanitized = string.IsNullOrWhiteSpace(originalFileName) ? "arquivo" : originalFileName.Trim();
        var extension = Path.GetExtension(sanitized);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
        var requestCode = history?.Id ?? Id;

        var safeBase = string.IsNullOrWhiteSpace(nameWithoutExtension)
            ? "arquivo"
            : nameWithoutExtension;

        var suffix = $"-req-{requestCode}";
        if (safeBase.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(extension)
                ? safeBase
                : safeBase + extension;
        }

        return string.IsNullOrWhiteSpace(extension)
            ? safeBase + suffix
            : safeBase + suffix + extension;
    }

    private async Task CopyToClipboard(string text)
    {
        await Js.InvokeVoidAsync("copyToClipboard", text);
    }

    private async Task CopyRequestPayload()
    {
        if (history == null)
        {
            return;
        }

        await CopyToClipboard(BuildRequestPayloadText());
    }

    private async Task CopyGeneralInfo()
    {
        if (history == null)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Method: {history.Method}");
        sb.AppendLine($"Route: {history.Route}");
        sb.AppendLine($"Data/Hora: {TimezoneService.FormatLocalTime(history.RequestedAt, "dd/MM/yyyy HH:mm:ss")}");
        sb.AppendLine($"StatusCode: {history.ResponseStatusCode}");
        sb.AppendLine($"DelayMs: {history.DelayMs}");
        sb.AppendLine($"ClientIp: {history.ClientIp}");
        if (history.MockEndpoint != null)
        {
            sb.AppendLine($"Mock: {history.MockEndpoint.Description}");
        }

        await CopyToClipboard(sb.ToString().TrimEnd());
    }

    private async Task CopyRequestHeaders()
    {
        await CopyToClipboard(FormatDictionaryForCopy("Request Headers", requestHeaders));
    }

    private async Task CopyQueryParams()
    {
        await CopyToClipboard(FormatDictionaryForCopy("Query Params", queryParams));
    }

    private async Task CopyRequestBody()
    {
        await CopyToClipboard(GetRequestBodyForCopy());
    }

    private async Task CopyResponseHeaders()
    {
        await CopyToClipboard(FormatDictionaryForCopy("Response Headers", responseHeaders));
    }

    private async Task CopyResponseBody()
    {
        await CopyToClipboard(GetResponseBodyForCopy());
    }

    private bool CanCopyRequestBody()
    {
        return !string.IsNullOrWhiteSpace(GetRequestBodyForCopy());
    }

    private bool CanCopyResponseBody()
    {
        return !string.IsNullOrWhiteSpace(GetResponseBodyForCopy());
    }

    private string BuildRequestPayloadText()
    {
        if (history == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("##### Informações Gerais");
        sb.AppendLine($"Method: {history.Method}");
        sb.AppendLine($"Route: {history.Route}");
        sb.AppendLine($"Data/Hora: {TimezoneService.FormatLocalTime(history.RequestedAt, "dd/MM/yyyy HH:mm:ss")}");
        sb.AppendLine($"StatusCode: {history.ResponseStatusCode}");
        sb.AppendLine($"DelayMs: {history.DelayMs}");
        sb.AppendLine($"ClientIp: {history.ClientIp}");
        if (history.MockEndpoint != null)
        {
            sb.AppendLine($"Mock: {history.MockEndpoint.Description}");
        }
        if (queryParams != null && queryParams.Count > 0)
        {
            sb.AppendLine("Query Params:");
            foreach (var param in queryParams.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"- {param.Key}: {param.Value}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("##### Request Headers");
        AppendDictionaryLines(sb, requestHeaders);

        sb.AppendLine();
        sb.AppendLine("##### Response Headers");
        AppendDictionaryLines(sb, responseHeaders);

        sb.AppendLine();
        sb.AppendLine("##### Request Body");
        var requestBody = GetRequestBodyForCopy();
        sb.AppendLine(string.IsNullOrWhiteSpace(requestBody) ? EmptyLabel : requestBody);

        sb.AppendLine();
        sb.AppendLine("##### Response Body");
        var responseBody = GetResponseBodyForCopy();
        sb.AppendLine(string.IsNullOrWhiteSpace(responseBody) ? EmptyLabel : responseBody);

        return sb.ToString().TrimEnd();
    }

    private string GetRequestBodyForCopy()
    {
        if (history == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(history.RequestFormJson))
        {
            return NormalizeJsonIfPossible(history.RequestFormJson);
        }

        if (!string.IsNullOrWhiteSpace(history.RequestBody))
        {
            return NormalizeJsonIfPossible(history.RequestBody);
        }

        if (!string.IsNullOrWhiteSpace(history.RequestBodyBase64))
        {
            return history.RequestBodyBase64;
        }

        return string.Empty;
    }

    private string GetResponseBodyForCopy()
    {
        if (history == null)
        {
            return string.Empty;
        }

        if (history.ResponseBinaryBlobId.HasValue)
        {
            return "[Response body armazenado como blob. Use o botao de download para obter o arquivo completo.]";
        }

        if (!string.IsNullOrWhiteSpace(history.ResponseBodyBase64))
        {
            return history.ResponseBodyBase64;
        }

        if (!string.IsNullOrWhiteSpace(history.ResponseBody) && !history.ResponseBody.StartsWith("[Response"))
        {
            return NormalizeJsonIfPossible(history.ResponseBody);
        }

        return string.Empty;
    }

    private static void AppendDictionaryLines(StringBuilder sb, Dictionary<string, string>? dict)
    {
        if (dict != null && dict.Count > 0)
        {
            foreach (var item in dict.OrderBy(v => v.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"{item.Key}: {item.Value}");
        }
        else
        {
            sb.AppendLine(EmptyLabel);
        }
    }

    private static string FormatDictionaryForCopy(string title, Dictionary<string, string>? values)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {title} ===");

        if (values == null || values.Count == 0)
        {
            sb.AppendLine(EmptyLabel);
            return sb.ToString().TrimEnd();
        }

        foreach (var item in values.OrderBy(v => v.Key, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"{item.Key}: {item.Value}");
        }

        return sb.ToString().TrimEnd();
    }

    private static readonly JsonSerializerOptions _indentedWriteOptions = new() { WriteIndented = true };

    private static string NormalizeJsonIfPossible(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(value);
            return JsonSerializer.Serialize(doc.RootElement, _indentedWriteOptions);
        }
        catch
        {
            return value;
        }
    }

    private void VoltarParaLista()
    {
        Navigation.NavigateTo("/historico");
    }

    private void EditarMock()
    {
        if (history?.MockEndpoint == null) return;
        var returnUrl = Uri.EscapeDataString($"/historico/{history.Id}");
        Navigation.NavigateTo($"/mock/edit/{history.MockEndpoint.Id}?returnUrl={returnUrl}");
    }

    private static Dictionary<string, string> ParseQueryString(string qs)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (qs.StartsWith('?')) qs = qs[1..];
        foreach (var part in qs.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx > 0)
            {
                result[Uri.UnescapeDataString(part[..idx])] = Uri.UnescapeDataString(part[(idx + 1)..]);
            }
            else
            {
                result[Uri.UnescapeDataString(part)] = string.Empty;
            }
        }
        return result;
    }

    private async Task LoadResponseBlobContent()
    {
        if (history?.ResponseBinaryBlobId == null)
            return;

        loadingBlobContent = true;
        try
        {
            var blob = await BinaryStorage.GetAsync(history.ResponseBinaryBlobId.Value, CancellationToken.None);
            if (blob.HasValue)
            {
                responseBlobContent = blob.Value.bytes;
                responseBlobPreview = BuildPreviewState(blob.Value.bytes, blob.Value.contentType);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar blob: {ex.Message}");
        }
        finally
        {
            loadingBlobContent = false;
        }
    }

    private async Task DownloadResponseBlob()
    {
        if (history?.ResponseBinaryBlobId == null)
            return;

        try
        {
            var blob = await BinaryStorage.GetAsync(history.ResponseBinaryBlobId.Value, CancellationToken.None);
            if (blob.HasValue)
            {
                var base64 = Convert.ToBase64String(blob.Value.bytes);
                var contentType = blob.Value.contentType ?? "application/octet-stream";
                var fileNameBase = string.IsNullOrWhiteSpace(blob.Value.fileName)
                    ? BuildFallbackFileName("response", contentType)
                    : blob.Value.fileName!;

                var fileName = AppendRequestCodeToFileName(fileNameBase);
                await Js.InvokeVoidAsync(DownloadBase64File, base64, fileName, contentType);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao baixar blob: {ex.Message}");
        }
    }

    private async Task DownloadResponseBase64()
    {
        if (history == null || string.IsNullOrWhiteSpace(history.ResponseBodyBase64))
            return;

        var fileName = GetResponseFileName();
        var contentType = string.IsNullOrWhiteSpace(history.ResponseBodyContentType)
            ? "application/octet-stream"
            : history.ResponseBodyContentType;

        await Js.InvokeVoidAsync(DownloadBase64File, history.ResponseBodyBase64, fileName, contentType);
    }

    private async Task DownloadRequestBase64()
    {
        if (history == null || string.IsNullOrWhiteSpace(history.RequestBodyBase64))
            return;

        var fileNameBase = string.IsNullOrWhiteSpace(history.RequestBodyFileName)
            ? BuildFallbackFileName("request", history.RequestBodyContentType)
            : history.RequestBodyFileName;

        var fileName = AppendRequestCodeToFileName(fileNameBase);

        var contentType = string.IsNullOrWhiteSpace(history.RequestBodyContentType)
            ? "application/octet-stream"
            : history.RequestBodyContentType;

        await Js.InvokeVoidAsync(DownloadBase64File, history.RequestBodyBase64, fileName, contentType);
    }

    private async Task DownloadMultipartRequestFile(int index)
    {
        if (index < 0 || index >= requestMultipartFiles.Count)
            return;

        var file = requestMultipartFiles[index];
        var base64 = Convert.ToBase64String(file.Bytes);
        var fileName = AppendRequestCodeToFileName(file.FileName);
        await Js.InvokeVoidAsync(DownloadBase64File, base64, fileName, file.ContentType);
    }

    private async Task DownloadMultipartRequestFileAsBase64(int index)
    {
        if (index < 0 || index >= requestMultipartFiles.Count)
            return;

        var file = requestMultipartFiles[index];
        var base64Text = Convert.ToBase64String(file.Bytes);
        var textBytes = Encoding.UTF8.GetBytes(base64Text);
        var textBase64 = Convert.ToBase64String(textBytes);
        var outputName = AppendRequestCodeToFileName(file.FileName) + ".base64.txt";

        await Js.InvokeVoidAsync(DownloadBase64File, textBase64, outputName, "text/plain;charset=utf-8");
    }

    private async Task DownloadAllMultipartRequestFiles()
    {
        for (var i = 0; i < requestMultipartFiles.Count; i++)
        {
            await DownloadMultipartRequestFile(i);
        }
    }

    private static string BuildFallbackFileName(string prefix, string? contentType)
    {
        var extension = GuessExtensionFromContentType(contentType);
        return string.IsNullOrWhiteSpace(extension)
            ? $"{prefix}.bin"
            : $"{prefix}.{extension}";
    }

    private static string? GuessExtensionFromContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        var separatorIndex = contentType.IndexOf(';');
        var normalized = (separatorIndex >= 0 ? contentType[..separatorIndex] : contentType)
            .Trim()
            .ToLowerInvariant();

        return normalized switch
        {
            "application/pdf" => "pdf",
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "image/bmp" => "bmp",
            "image/tiff" => "tiff",
            "application/json" => "json",
            "text/plain" => "txt",
            "text/xml" => "xml",
            "application/xml" => "xml",
            _ when normalized.StartsWith("text/") => "txt",
            _ => null
        };
    }

    private static bool IsMultipartFormData(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType)
               && contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase)
               && contentType.Contains("boundary=", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<MultipartRequestFile>> ExtractMultipartRequestFilesAsync(string base64Body, string? contentType)
    {
        var files = new List<MultipartRequestFile>();
        if (!IsMultipartFormData(contentType))
            return files;

        string boundary;
        try
        {
            var mediaType = MediaTypeHeaderValue.Parse(contentType!);
            boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value ?? string.Empty;
        }
        catch
        {
            return files;
        }

        if (string.IsNullOrWhiteSpace(boundary))
            return files;

        byte[] rawBytes;
        try
        {
            rawBytes = Convert.FromBase64String(base64Body);
        }
        catch
        {
            return files;
        }

        using var stream = new MemoryStream(rawBytes);
        var reader = new MultipartReader(boundary, stream);

        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync()) != null)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                continue;

            var fileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileNameStar).Value;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileName).Value;
            }

            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            var fieldName = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value ?? "file";
            var partContentType = string.IsNullOrWhiteSpace(section.ContentType)
                ? "application/octet-stream"
                : section.ContentType;

            using var ms = new MemoryStream();
            await section.Body.CopyToAsync(ms);
            var bytes = ms.ToArray();

            files.Add(new MultipartRequestFile(
                fieldName,
                fileName,
                partContentType,
                bytes,
                BuildPreviewState(bytes, partContentType)));
        }

        return files;
    }

    private static BinaryPreviewState? BuildPreviewFromBase64(string? base64Value, string? declaredContentType)
    {
        if (string.IsNullOrWhiteSpace(base64Value))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(base64Value);
            return BuildPreviewState(bytes, declaredContentType);
        }
        catch
        {
            return new BinaryPreviewState(
                false,
                false,
                string.IsNullOrWhiteSpace(declaredContentType) ? "application/octet-stream" : declaredContentType,
                null,
                0,
                "Base64 invalido para gerar pre-visualizacao.");
        }
    }

    private static BinaryPreviewState BuildPreviewState(byte[] bytes, string? declaredContentType)
    {
        var assessment = BinaryContentInspector.AssessForInlinePreview(bytes, declaredContentType);
        var dataUrl = assessment.CanInlinePreview
            ? $"data:{assessment.EffectiveContentType};base64,{Convert.ToBase64String(bytes)}"
            : null;

        return new BinaryPreviewState(
            assessment.CanInlinePreview,
            assessment.IsPdf,
            assessment.EffectiveContentType,
            dataUrl,
            bytes.LongLength,
            assessment.BlockReason);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private async Task<bool> ConfirmActionAsync(string title, string message, string icon, string iconColor)
    {
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), message },
            { nameof(ConfirmDialog.Icon), icon },
            { nameof(ConfirmDialog.IconColor), iconColor }
        };

        var modal = Modal.Show<ConfirmDialog>(title, parameters, new ModalOptions { Size = ModalSize.Small });
        var result = await modal.Result;
        return !result.Cancelled;
    }
}
