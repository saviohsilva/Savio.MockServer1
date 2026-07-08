using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;
using System.Text;
using System.Text.Json;

namespace Savio.MockServer.Pages;

public partial class HistoricoDetalhes
{
    private const string EmptyLabel = "(vazio)";
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
    private byte[]? responseBlobContent = null;
    private bool loadingBlobContent = false;

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
        if (!string.IsNullOrWhiteSpace(history?.ResponseBodyFileName))
        {
            return history.ResponseBodyFileName;
        }

        return "response.bin";
    }

    private string GetResponseDownloadUrl()
    {
        var contentType = string.IsNullOrWhiteSpace(history?.ResponseBodyContentType)
            ? "application/octet-stream"
            : history.ResponseBodyContentType;

        return $"data:{contentType};base64,{history?.ResponseBodyBase64}";
    }

    private async Task CopyToClipboard(string text)
    {
        try
        {
            await Js.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
        catch
        {
            // Clipboard API may be unavailable in non-secure contexts; ignore
        }
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
            if (responseBlobContent != null && IsTextContent(history.ResponseBodyContentType))
            {
                return GetTextPreview(responseBlobContent);
            }

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
                var fileName = blob.Value.fileName ?? "response.bin";

                var url = $"data:{contentType};base64,{base64}";
                await Js.InvokeVoidAsync("eval", $"var a = document.createElement('a'); a.href = '{url}'; a.download = '{fileName}'; a.click();");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao baixar blob: {ex.Message}");
        }
    }

    private static bool IsTextContent(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.Contains("text/") ||
               contentType.Contains("json") ||
               contentType.Contains("xml") ||
               contentType.Contains("javascript");
    }

    private static string GetTextPreview(byte[] bytes)
    {
        try
        {
            var text = Encoding.UTF8.GetString(bytes);
            return text.Length > 10000 ? text[..10000] + "\n\n... (truncado)" : text;
        }
        catch
        {
            return "[Não foi possível decodificar como texto UTF-8]";
        }
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
