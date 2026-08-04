using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Savio.MockServer.Components;

public partial class CurlExportDialog
{
    [CascadingParameter]
    BlazoredModalInstance BlazoredModal { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public string CurlText { get; set; } = string.Empty;

    [Parameter]
    public List<string> Warnings { get; set; } = [];

    private bool copied;

    private async Task Copy()
    {
        await JS.InvokeVoidAsync("copyToClipboard", CurlText);
        copied = true;
    }

    private async Task Close() =>
        await BlazoredModal.CloseAsync();
}
