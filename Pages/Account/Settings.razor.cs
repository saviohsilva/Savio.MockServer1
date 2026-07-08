using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Security;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages.Account;

public partial class Settings
{
    private const string TextDangerClass = "text-danger";
    private const string TextSuccessClass = "text-success";
    private const string ProtectedAdminUserName = "mockadmin";

    [CascadingParameter]
    public IModalService Modal { get; set; } = default!;

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AliasService AliasService { get; set; } = default!;
    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;
    [Inject] private IEmailSender EmailSender { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private EmailSettingService EmailSettingService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private ApplicationUser? user;
    private bool mfaEnabled;
    private bool isEditingAlias;
    private bool isSavingAlias;
    private string newAlias = string.Empty;
    private string? aliasMessage;
    private string aliasMessageClass = TextDangerClass;
    private bool isCurrentUserAdmin;
    private string? adminManageMessage;
    private string adminManageMessageClass = TextDangerClass;
    private List<AdminUserItem> adminUsers = [];
    private bool isEmailConfigured;
    private bool isAppsettingsActive;
    private string? sendingResetUserId;
    private string? adminResetMessage;
    private string adminResetMessageClass = TextDangerClass;

    // Email settings form (admin only)
    private string emailSmtpHost = string.Empty;
    private int emailSmtpPort = 587;
    private string emailSmtpUser = string.Empty;
    private string emailSmtpPass = string.Empty;
    private string emailFromEmail = string.Empty;
    private string emailFromName = string.Empty;
    private bool hasExistingPassword;
    private bool isSavingEmailSettings;
    private string? emailSettingsMessage;
    private string emailSettingsMessageClass = TextDangerClass;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState!;
        user = await UserManager.GetUserAsync(authState.User);
        if (user != null)
        {
            mfaEnabled = await UserManager.GetTwoFactorEnabledAsync(user);
            isCurrentUserAdmin = await UserManager.IsInRoleAsync(user, AppRoles.Admin);
            isEmailConfigured = await EmailSettingService.IsEmailConfiguredAsync();
            isAppsettingsActive = EmailSettingService.IsAppsettingsActive;

            if (isCurrentUserAdmin)
            {
                await LoadAdminUsersAsync();
                await LoadEmailSettingsAsync();
            }
        }
    }

    private async Task LoadAdminUsersAsync()
    {
        if (!isCurrentUserAdmin)
        {
            adminUsers = [];
            return;
        }

        var admins = await UserManager.GetUsersInRoleAsync(AppRoles.Admin);
        var adminIds = admins.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);

        adminUsers = await UserManager.Users
            .OrderBy(u => u.UserName)
            .Select(u => new AdminUserItem
            {
                Id = u.Id,
                UserName = u.UserName ?? "(sem usuário)",
                Email = u.Email,
                Alias = u.Alias,
                IsAdmin = false,
                IsCurrentUser = user != null && u.Id == user.Id,
                IsProtectedAdmin = string.Equals(u.UserName, ProtectedAdminUserName, StringComparison.OrdinalIgnoreCase),
                MfaEnabled = u.TwoFactorEnabled
            })
            .ToListAsync();

        foreach (var adminUser in adminUsers)
            adminUser.IsAdmin = adminIds.Contains(adminUser.Id);
    }

    private async Task ToggleAdmin(AdminUserItem target, bool requestedIsAdmin)
    {
        adminManageMessage = null;

        if (user == null || !isCurrentUserAdmin)
            return;

        if (target.Id == user.Id && !requestedIsAdmin)
        {
            target.IsAdmin = true;
            adminManageMessage = "Você não pode remover seu próprio perfil de administrador.";
            adminManageMessageClass = TextDangerClass;
            return;
        }

        if (target.IsAdmin == requestedIsAdmin)
            return;

        var targetUser = await UserManager.FindByIdAsync(target.Id);
        if (targetUser == null)
        {
            adminManageMessage = "Usuário não encontrado.";
            adminManageMessageClass = TextDangerClass;
            return;
        }

        var isProtectedAdmin = target.IsProtectedAdmin ||
                               string.Equals(targetUser.UserName, ProtectedAdminUserName, StringComparison.OrdinalIgnoreCase);
        if (!requestedIsAdmin && isProtectedAdmin)
        {
            target.IsAdmin = true;
            adminManageMessage = $"A permissão de administrador do usuário '{ProtectedAdminUserName}' não pode ser removida.";
            adminManageMessageClass = TextDangerClass;
            return;
        }

        if (!await ConfirmAdminToggleAsync(target, requestedIsAdmin))
            return;

        IdentityResult result;
        if (requestedIsAdmin)
            result = await UserManager.AddToRoleAsync(targetUser, AppRoles.Admin);
        else
            result = await UserManager.RemoveFromRoleAsync(targetUser, AppRoles.Admin);

        if (!result.Succeeded)
        {
            target.IsAdmin = !requestedIsAdmin;
            adminManageMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            adminManageMessageClass = TextDangerClass;
            return;
        }

        target.IsAdmin = requestedIsAdmin;

        adminManageMessage = target.IsAdmin
            ? $"{target.UserName} agora é administrador."
            : $"{target.UserName} removido do perfil de administrador.";
        adminManageMessageClass = TextSuccessClass;
    }

    private async Task SendPasswordResetToUser(AdminUserItem target)
    {
        adminResetMessage = null;

        if (!isCurrentUserAdmin)
            return;

        if (!isEmailConfigured)
        {
            adminResetMessage = "SMTP não configurado. Configure Email:SmtpHost para habilitar envio de redefinição de senha.";
            adminResetMessageClass = TextDangerClass;
            return;
        }

        if (string.IsNullOrWhiteSpace(target.Id))
        {
            adminResetMessage = "Selecione um usuário válido para enviar o e-mail de redefinição.";
            adminResetMessageClass = TextDangerClass;
            return;
        }

        var targetUser = await UserManager.FindByIdAsync(target.Id);
        if (targetUser == null)
        {
            adminResetMessage = "Usuário não encontrado.";
            adminResetMessageClass = TextDangerClass;
            return;
        }

        if (string.IsNullOrWhiteSpace(targetUser.Email))
        {
            adminResetMessage = "O usuário selecionado não possui e-mail e não pode receber redefinição automática.";
            adminResetMessageClass = TextDangerClass;
            return;
        }

        var confirmationMessage =
            $"Confirma o envio do e-mail de redefinição de senha para {targetUser.UserName} ({targetUser.Email})?";
        var confirmed = await ConfirmActionAsync(
            "Confirmar Envio",
            confirmationMessage,
            "bi-envelope-check",
            "primary");
        if (!confirmed)
            return;

        sendingResetUserId = target.Id;
        StateHasChanged();

        try
        {
            var code = await UserManager.GeneratePasswordResetTokenAsync(targetUser);
            var callbackUrl =
                $"{Navigation.BaseUri}account/reset-password?email={Uri.EscapeDataString(targetUser.Email)}&code={Uri.EscapeDataString(code)}";

            await EmailSender.SendEmailAsync(
                targetUser.Email,
                "Redefinição de senha — Savio Mock Server",
                "<h3>Redefinição de senha</h3>" +
                "<p>Um administrador solicitou uma redefinição de senha para sua conta.</p>" +
                $"<p><a href='{callbackUrl}'>Clique aqui para redefinir sua senha</a></p>" +
                "<p>Se você não esperava esse e-mail, entre em contato com o administrador.</p>");

            adminResetMessage = $"E-mail de redefinição enviado para {targetUser.Email}.";
            adminResetMessageClass = TextSuccessClass;
        }
        catch (Exception ex)
        {
            adminResetMessage = $"Erro ao enviar e-mail de redefinição: {ex.Message}";
            adminResetMessageClass = TextDangerClass;
        }
        finally
        {
            sendingResetUserId = null;
        }
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
                aliasMessageClass = TextDangerClass;
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
                aliasMessageClass = TextDangerClass;
                return;
            }

            user.Alias = alias;
            var result = await UserManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                isEditingAlias = false;
                aliasMessage = "Alias atualizado com sucesso!";
                aliasMessageClass = TextSuccessClass;
            }
            else
            {
                aliasMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                aliasMessageClass = TextDangerClass;
            }
        }
        catch (Exception ex)
        {
            aliasMessage = $"Erro ao atualizar alias: {ex.Message}";
            aliasMessageClass = TextDangerClass;
        }
        finally
        {
            isSavingAlias = false;
        }
    }

    private sealed class AdminUserItem
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Alias { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsCurrentUser { get; set; }
        public bool IsProtectedAdmin { get; set; }
        public bool MfaEnabled { get; set; }
    }

    private async Task LoadEmailSettingsAsync()
    {
        var entity = await EmailSettingService.GetDbSettingsAsync();
        if (entity != null)
        {
            emailSmtpHost = entity.SmtpHost ?? string.Empty;
            emailSmtpPort = entity.SmtpPort;
            emailSmtpUser = entity.SmtpUser ?? string.Empty;
            emailFromEmail = entity.FromEmail ?? string.Empty;
            emailFromName = entity.FromName ?? string.Empty;
            hasExistingPassword = !string.IsNullOrEmpty(entity.SmtpPassEncrypted);
        }
        else
        {
            emailSmtpPort = 587;
        }
    }

    private async Task SaveEmailSettings()
    {
        if (!isCurrentUserAdmin || user == null)
            return;

        emailSettingsMessage = null;
        isSavingEmailSettings = true;
        StateHasChanged();

        try
        {
            // Passa null na senha para "não alterar" caso o campo esteja vazio e já exista senha
            string? existingOrEmpty = hasExistingPassword ? null : string.Empty;
            string? passToSave = string.IsNullOrEmpty(emailSmtpPass) ? existingOrEmpty : emailSmtpPass;

            await EmailSettingService.SaveAsync(
                string.IsNullOrWhiteSpace(emailSmtpHost) ? null : emailSmtpHost,
                emailSmtpPort,
                string.IsNullOrWhiteSpace(emailSmtpUser) ? null : emailSmtpUser,
                passToSave,
                string.IsNullOrWhiteSpace(emailFromEmail) ? null : emailFromEmail,
                string.IsNullOrWhiteSpace(emailFromName) ? null : emailFromName,
                user.Id);

            isEmailConfigured = await EmailSettingService.IsEmailConfiguredAsync();
            hasExistingPassword = !string.IsNullOrEmpty(emailSmtpPass) || hasExistingPassword;
            emailSmtpPass = string.Empty;

            emailSettingsMessage = "Configurações de e-mail salvas com sucesso!";
            emailSettingsMessageClass = TextSuccessClass;
        }
        catch (Exception ex)
        {
            emailSettingsMessage = $"Erro ao salvar configurações: {ex.Message}";
            emailSettingsMessageClass = TextDangerClass;
        }
        finally
        {
            isSavingEmailSettings = false;
        }
    }

    private async Task<bool> ConfirmAdminToggleAsync(AdminUserItem target, bool requestedIsAdmin)
    {
        if (requestedIsAdmin)
        {
            var confirmed = await ConfirmActionAsync(
                "Confirmar Promoção",
                $"Confirma ativar o perfil de administrador para {target.UserName}?",
                "bi-shield-check",
                "warning");
            if (!confirmed)
            {
                target.IsAdmin = false;
                return false;
            }
        }
        else
        {
            var confirmed = await ConfirmActionAsync(
                "Confirmar Remoção",
                $"Confirma remover o perfil de administrador de {target.UserName}?",
                "bi-shield-x",
                "danger");
            if (!confirmed)
            {
                target.IsAdmin = true;
                return false;
            }
        }
        return true;
    }

    private async Task<bool> ConfirmActionAsync(string title, string message, string icon, string iconColor)
    {
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), message },
            { nameof(ConfirmDialog.Icon), icon },
            { nameof(ConfirmDialog.IconColor), iconColor }
        };

        var modal = Modal.Show<ConfirmDialog>(title, parameters, new ModalOptions { Size = ModalSize.Small });
        var result = await modal.Result;
        return !result.Cancelled;
    }

    private async Task DisableMfaForUser(AdminUserItem target)
    {
        adminManageMessage = null;

        if (user == null || !isCurrentUserAdmin)
            return;

        if (target.IsCurrentUser)
        {
            // Para o próprio usuário, usa a página de configurações de MFA
            adminManageMessage = "Para gerenciar seu próprio MFA, use a página de segurança abaixo.";
            adminManageMessageClass = TextDangerClass;
            return;
        }

        var targetUser = await UserManager.FindByIdAsync(target.Id);
        if (targetUser == null)
        {
            adminManageMessage = "Usuário não encontrado.";
            adminManageMessageClass = TextDangerClass;
            return;
        }

        var confirmed = await ConfirmActionAsync(
            "Desabilitar MFA",
            $"Confirma desabilitar o MFA do usuário {target.UserName}? O usuário precisará configurar o MFA novamente caso queira reativá-lo.",
            "bi-shield-x",
            "danger");

        if (!confirmed)
            return;

        await UserManager.SetTwoFactorEnabledAsync(targetUser, false);
        await UserManager.ResetAuthenticatorKeyAsync(targetUser);

        target.MfaEnabled = false;
        adminManageMessage = $"MFA do usuário {target.UserName} desabilitado com sucesso.";
        adminManageMessageClass = TextSuccessClass;
    }
}
