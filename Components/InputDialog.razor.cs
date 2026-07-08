using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Savio.MockServer.Components;

public partial class InputDialog
{
    [CascadingParameter]
    BlazoredModalInstance BlazoredModal { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = "Digite o valor:";

    [Parameter]
    public string Placeholder { get; set; } = "";

    [Parameter]
    public string InputType { get; set; } = "password";

    [Parameter]
    public string Icon { get; set; } = "bi-lock";

    [Parameter]
    public string IconColor { get; set; } = "primary";

    private string inputValue = "";

    private async Task Confirm() =>
        await BlazoredModal.CloseAsync(ModalResult.Ok(inputValue));

    private async Task Cancel() =>
        await BlazoredModal.CancelAsync();

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await Confirm();
        else if (e.Key == "Escape")
            await Cancel();
    }
}
