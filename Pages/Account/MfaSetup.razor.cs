using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.JSInterop;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Pages.Account;

public partial class MfaSetup
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IEmailSender EmailSender { get; set; } = default!;
    [Inject] private IWebHostEnvironment Env { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private bool mfaEnabled;
    private string sharedKey = string.Empty;
    private string qrCodeUri = string.Empty;
    private string verificationCode = string.Empty;
    private string? statusMessage;
    private string statusClass = "alert-info";
    private bool isLoading;
    private IEnumerable<string>? recoveryCodes;
    private string selectedMethod = "Authenticator";
    private bool emailCodeSent;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState!;
        var user = await UserManager.GetUserAsync(authState.User);
        if (user == null) return;

        mfaEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

        if (!mfaEnabled)
            await LoadSharedKeyAndQrCodeUri(user);
    }

    private async Task LoadSharedKeyAndQrCodeUri(ApplicationUser user)
    {
        var key = await UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await UserManager.ResetAuthenticatorKeyAsync(user);
            key = await UserManager.GetAuthenticatorKeyAsync(user);
        }

        sharedKey = FormatKey(key!);
        qrCodeUri = $"otpauth://totp/SavioMockServer:{user.Email}?secret={key}&issuer=SavioMockServer&digits=6";
    }

    private async Task EnableMfa()
    {
        isLoading = true;
        statusMessage = null;
        StateHasChanged();

        try
        {
            var authState = await AuthState!;
            var user = await UserManager.GetUserAsync(authState.User);
            if (user == null) return;

            var code = verificationCode.Replace(" ", "").Replace("-", "");
            var isValid = await UserManager.VerifyTwoFactorTokenAsync(
                user, UserManager.Options.Tokens.AuthenticatorTokenProvider, code);

            if (isValid)
            {
                await UserManager.SetTwoFactorEnabledAsync(user, true);
                user.MfaMethod = "Authenticator";
                await UserManager.UpdateAsync(user);
                recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                mfaEnabled = true;
                statusMessage = "MFA habilitado com sucesso! Guarde os códigos de recuperação.";
                statusClass = "alert-success";
            }
            else
            {
                statusMessage = "Código inválido. Verifique e tente novamente.";
                statusClass = "alert-danger";
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task DisableMfa()
    {
        var authState = await AuthState!;
        var user = await UserManager.GetUserAsync(authState.User);
        if (user == null) return;

        await UserManager.SetTwoFactorEnabledAsync(user, false);
        await UserManager.ResetAuthenticatorKeyAsync(user);
        mfaEnabled = false;
        recoveryCodes = null;
        await LoadSharedKeyAndQrCodeUri(user);
        statusMessage = "MFA desabilitado.";
        statusClass = "alert-warning";
    }

    private async Task GenerateNewRecoveryCodes()
    {
        var authState = await AuthState!;
        var user = await UserManager.GetUserAsync(authState.User);
        if (user == null) return;

        recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        statusMessage = "Novos códigos de recuperação gerados.";
        statusClass = "alert-success";
    }

    private async Task CopyToClipboard(string text)
    {
        await JS.InvokeVoidAsync("copyToClipboard", text);
    }

    private async Task SendEmailTestCode()
    {
        isLoading = true;
        statusMessage = null;
        StateHasChanged();

        try
        {
            var authState = await AuthState!;
            var user = await UserManager.GetUserAsync(authState.User);
            if (user == null) return;

            var code = await UserManager.GenerateTwoFactorTokenAsync(user, "Email");

            await EmailSender.SendEmailAsync(
                user.Email!,
                "Código de verificação MFA — Savio Mock Server",
                $"<h3>Código de verificação</h3>" +
                $"<p>Seu código de verificação é: <strong>{code}</strong></p>" +
                $"<p>Este código expira em 10 minutos.</p>");

            emailCodeSent = true;
            statusMessage = "Código enviado para seu e-mail! Verifique sua caixa de entrada.";
            statusClass = "alert-info";
        }
        catch (Exception ex)
        {
            statusMessage = $"Erro ao enviar código: {ex.Message}";
            statusClass = "alert-danger";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task EnableMfaEmail()
    {
        isLoading = true;
        statusMessage = null;
        StateHasChanged();

        try
        {
            var authState = await AuthState!;
            var user = await UserManager.GetUserAsync(authState.User);
            if (user == null) return;

            var code = verificationCode.Replace(" ", "").Replace("-", "");
            var isValid = await UserManager.VerifyTwoFactorTokenAsync(user, "Email", code);

            if (isValid)
            {
                await UserManager.SetTwoFactorEnabledAsync(user, true);
                user.MfaMethod = "Email";
                await UserManager.UpdateAsync(user);
                recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                mfaEnabled = true;
                statusMessage = "MFA por e-mail habilitado com sucesso! Guarde os códigos de recuperação.";
                statusClass = "alert-success";
            }
            else
            {
                statusMessage = "Código inválido. Verifique e tente novamente.";
                statusClass = "alert-danger";
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    private static string FormatKey(string key)
    {
        var result = new System.Text.StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < key.Length)
        {
            result.Append(key.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < key.Length)
            result.Append(key.AsSpan(currentPosition));

        return result.ToString().ToLowerInvariant();
    }
}
