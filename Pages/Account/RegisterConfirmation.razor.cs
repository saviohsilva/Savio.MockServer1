using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Pages.Account;

public partial class RegisterConfirmation
{
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [SupplyParameterFromQuery]
    public string? AutoConfirmed { get; set; }
}
