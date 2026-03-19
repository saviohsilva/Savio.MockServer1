using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Shared;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/account/login");
    }
}
