using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Components;

public partial class ConfirmDialog
{
    [CascadingParameter]
    BlazoredModalInstance BlazoredModal { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = "Tem certeza?";

    [Parameter]
    public string Icon { get; set; } = "bi-question-circle";

    [Parameter]
    public string IconColor { get; set; } = "primary";

    private async Task Confirm() => await BlazoredModal.CloseAsync(ModalResult.Ok(true));
    private async Task Cancel() => await BlazoredModal.CancelAsync();
}
