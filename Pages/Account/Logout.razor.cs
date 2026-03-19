using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Pages.Account;

public partial class Logout
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/account/do-logout", forceLoad: true);
    }
}
