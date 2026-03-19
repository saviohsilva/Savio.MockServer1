using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;
using System.Text;
using System.Text.Json;

namespace Savio.MockServer.Pages;

public partial class HistoricoDetalhes
{
    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    private bool isLoading = true;
    private RequestHistoryEntity? history;
    private Dictionary<string, string>? requestHeaders;
    private Dictionary<string, string>? responseHeaders;
    private byte[]? responseBlobContent;
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
            }
            catch
            {
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

        var confirmed = await Js.InvokeAsync<bool>("confirm", "Confirma a exclusão deste item do histórico?");
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
        }
    }

    private void VoltarParaLista()
    {
        Navigation.NavigateTo("/historico");
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

    private bool IsTextContent(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.Contains("text/") ||
               contentType.Contains("json") ||
               contentType.Contains("xml") ||
               contentType.Contains("javascript");
    }

    private string GetTextPreview(byte[] bytes)
    {
        try
        {
            var text = Encoding.UTF8.GetString(bytes);
            return text.Length > 10000 ? text.Substring(0, 10000) + "\n\n... (truncado)" : text;
        }
        catch
        {
            return "[Não foi possível decodificar como texto UTF-8]";
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
