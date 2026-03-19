using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages.Account;

public partial class Settings
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AliasService AliasService { get; set; } = default!;
    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private ApplicationUser? user;
    private bool mfaEnabled;
    private bool isEditingAlias;
    private bool isSavingAlias;
    private string newAlias = string.Empty;
    private string? aliasMessage;
    private string aliasMessageClass = "text-danger";

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState!;
        user = await UserManager.GetUserAsync(authState.User);
        if (user != null)
            mfaEnabled = await UserManager.GetTwoFactorEnabledAsync(user);
    }

    private void StartEditAlias()
    {
        isEditingAlias = true;
        newAlias = user!.Alias;
        aliasMessage = null;
    }

    private void CancelEditAlias()
    {
        isEditingAlias = false;
        aliasMessage = null;
    }

    private async Task SaveAlias()
    {
        isSavingAlias = true;
        aliasMessage = null;
        StateHasChanged();

        try
        {
            var alias = newAlias.Trim().ToLowerInvariant();

            if (!AliasService.IsValidAliasFormat(alias))
            {
                aliasMessage = "O alias deve conter entre 3 e 50 caracteres, apenas letras minúsculas, números, hífens e underscores. Deve começar e terminar com letra ou número.";
                aliasMessageClass = "text-danger";
                return;
            }

            if (alias == user!.Alias)
            {
                isEditingAlias = false;
                return;
            }

            var aliasAvailable = await AliasService.IsAliasAvailableAsync(alias, user.Id);
            if (!aliasAvailable)
            {
                aliasMessage = $"O alias '{alias}' já está em uso por outro usuário. Escolha outro.";
                aliasMessageClass = "text-danger";
                return;
            }

            user.Alias = alias;
            var result = await UserManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                isEditingAlias = false;
                aliasMessage = "Alias atualizado com sucesso!";
                aliasMessageClass = "text-success";
            }
            else
            {
                aliasMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                aliasMessageClass = "text-danger";
            }
        }
        catch (Exception ex)
        {
            aliasMessage = $"Erro ao atualizar alias: {ex.Message}";
            aliasMessageClass = "text-danger";
        }
        finally
        {
            isSavingAlias = false;
        }
    }
}
