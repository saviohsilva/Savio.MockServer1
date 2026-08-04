using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Savio.MockServer.Helpers;

namespace Savio.MockServer.Components;

public partial class CurlImportDialog
{
    [CascadingParameter]
    BlazoredModalInstance BlazoredModal { get; set; } = default!;

    private string curlText = string.Empty;
    private string? errorMessage;

    private async Task Import()
    {
        var result = CurlHelper.Parse(curlText);
        if (result == null)
        {
            errorMessage = "Não foi possível interpretar o comando cURL informado.";
            return;
        }

        await BlazoredModal.CloseAsync(ModalResult.Ok(result));
    }

    private async Task Cancel() =>
        await BlazoredModal.CancelAsync();
}
