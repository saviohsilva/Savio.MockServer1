using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class AuthConfigs
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private List<MockAuthConfig> configs = [];
    private bool isLoading = true;
    private MockAuthConfig? configToDelete;
    private string? alertMessage;
    private string alertClass = "alert-success";
    private string alertIcon = "bi-check-circle";
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var auth = await AuthState;
            var user = await UserManager.GetUserAsync(auth.User);
            currentUserId = user?.Id;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            configs = await AuthConfigService.GetAllAsync(currentUserId);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ConfirmDelete(MockAuthConfig cfg) => configToDelete = cfg;

    private async Task ExecuteDelete()
    {
        if (configToDelete == null) return;
        await AuthConfigService.DeleteAsync(configToDelete.Id);
        configToDelete = null;
        ShowAlert("Configuração removida com sucesso.", true);
        await LoadAsync();
    }

    private void ShowAlert(string message, bool success)
    {
        alertMessage = message;
        alertClass = success ? "alert-success" : "alert-danger";
        alertIcon = success ? "bi-check-circle" : "bi-exclamation-triangle";
    }

    private static string GetTypeBadgeClass(MockAuthType type) => type switch
    {
        MockAuthType.Basic => "bg-secondary",
        MockAuthType.Bearer => "bg-primary",
        MockAuthType.ApiKey => "bg-success",
        _ => "bg-secondary"
    };

    private static string GetTypeIcon(MockAuthType type) => type switch
    {
        MockAuthType.Basic => "bi-person-lock",
        MockAuthType.Bearer => "bi-shield-check",
        MockAuthType.ApiKey => "bi-key",
        _ => "bi-lock"
    };
}
