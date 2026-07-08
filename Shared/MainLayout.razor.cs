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
    protected static int CurrentYear => DateTime.Now.Year;
    private string serverUrl = string.Empty;
    private bool isSidebarCollapsed;

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
            try
            {
                currentTheme = await JS.InvokeAsync<string>("getTheme") ?? string.Empty;
            }
            catch
            {
                currentTheme = string.Empty;
            }

            try
            {
                isSidebarCollapsed = await JS.InvokeAsync<bool>("getSidebarCollapsedState");
            }
            catch
            {
                isSidebarCollapsed = false;
            }

            try
            {
                var offset = await JS.InvokeAsync<int>("getBrowserTimezoneOffsetMinutes");
                TimezoneService.SetOffset(offset);
            }
            catch
            {
                // mantém UTC como fallback
            }

            StateHasChanged();
        }

        // Inicializa handles de redimensionamento em todas as tabelas presentes no DOM.
        // Executado em cada render (não só firstRender) para cobrir navegações entre páginas.
        // A função JS tem guarda interna e não re-inicializa tabelas já processadas.
        try
        {
            await JS.InvokeVoidAsync("initializeResizableTables");
        }
        catch
        {
            // comportamento opcional de UI
        }
    }

    private async Task OnThemeChanged()
    {
        await JS.InvokeVoidAsync("setTheme", currentTheme);
    }

    private async Task ToggleNavMenu()
    {
        isSidebarCollapsed = !isSidebarCollapsed;
        await JS.InvokeVoidAsync("setSidebarCollapsedState", isSidebarCollapsed);
    }
}
