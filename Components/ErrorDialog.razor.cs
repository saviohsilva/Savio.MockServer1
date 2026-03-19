using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Components;

public partial class ErrorDialog
{
    [CascadingParameter]
    BlazoredModalInstance BlazoredModal { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = "Ocorreu um erro.";

    [Parameter]
    public List<string>? Details { get; set; }

    private async Task Close() => await BlazoredModal.CancelAsync();
}
