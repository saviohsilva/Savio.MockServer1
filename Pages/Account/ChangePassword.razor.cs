using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Savio.MockServer.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Pages.Account;

public partial class ChangePassword
{
    private const string AlertDangerClass = "alert-danger";

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private ApplicationUser? user;
    protected bool isSubmitting;
    protected string? statusMessage;
    protected string statusMessageClass = AlertDangerClass;
    protected readonly CredentialUpdateModel credentialUpdateModel = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState!;
        user = await UserManager.GetUserAsync(authState.User);

        if (user == null)
            Navigation.NavigateTo("/account/login", forceLoad: true);
    }

    protected async Task HandleCredentialUpdate()
    {
        if (user == null)
            return;

        isSubmitting = true;
        statusMessage = null;
        StateHasChanged();

        try
        {
            if (credentialUpdateModel.NewSecret != credentialUpdateModel.ConfirmNewSecret)
            {
                statusMessage = "A nova senha e a confirmação não coincidem.";
                statusMessageClass = AlertDangerClass;
                return;
            }

            var result = await UserManager.ChangePasswordAsync(user, credentialUpdateModel.CurrentSecret, credentialUpdateModel.NewSecret);

            if (result.Succeeded)
            {
                await JSRuntime.InvokeVoidAsync("formNavigate", "/account/do-logout?credentialUpdated=true");
                return;
            }

            statusMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            statusMessageClass = AlertDangerClass;
        }
        catch (Exception ex)
        {
            statusMessage = $"Erro ao alterar senha: {ex.Message}";
            statusMessageClass = AlertDangerClass;
        }
        finally
        {
            isSubmitting = false;
        }
    }

    protected sealed class CredentialUpdateModel
    {
        [Required(ErrorMessage = "Senha atual é obrigatória")]
        public string CurrentSecret { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nova senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A nova senha deve ter no mínimo 6 caracteres")]
        public string NewSecret { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmação da nova senha é obrigatória")]
        public string ConfirmNewSecret { get; set; } = string.Empty;
    }
}
