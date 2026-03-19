using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Shared;

public partial class MainLayout
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private string currentTheme = string.Empty;
    protected int CurrentYear => DateTime.Now.Year;
    private string serverUrl = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        serverUrl = Navigation.BaseUri.TrimEnd('/');

        if (AuthState != null)
        {
            var state = await AuthState;
            var user = await UserManager.GetUserAsync(state.User);
            if (!string.IsNullOrEmpty(user?.Alias))
                serverUrl += $"/{user.Alias}";
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            currentTheme = await JS.InvokeAsync<string>("getTheme");
            var offset = await JS.InvokeAsync<int>("getBrowserTimezoneOffsetMinutes");
            TimezoneService.SetOffset(offset);
            StateHasChanged();
        }
    }

    private async Task OnThemeChanged()
    {
        await JS.InvokeVoidAsync("setTheme", currentTheme);
    }

    private void ToggleNavMenu()
    {
        // Toggle gerenciado via JavaScript em app.js
    }
}
