using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages.Account;

public partial class MfaVerify
{
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IEmailSender EmailSender { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string? errorMessage;
    private string mfaMethod = "Authenticator";

    protected override async Task OnInitializedAsync()
    {
        errorMessage = Error switch
        {
            "invalid" => "Código inválido. Tente novamente.",
            _         => null
        };

        var twoFactorUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (twoFactorUser != null)
        {
            mfaMethod = string.IsNullOrEmpty(twoFactorUser.MfaMethod) ? "Authenticator" : twoFactorUser.MfaMethod;

            if (mfaMethod == "Email" && string.IsNullOrEmpty(Error))
                await SendEmailCodeAsync(twoFactorUser);
        }
    }

    private async Task SendEmailCodeAsync(ApplicationUser user)
    {
        try
        {
            var code = await UserManager.GenerateTwoFactorTokenAsync(user, "Email");
            await EmailSender.SendEmailAsync(
                user.Email!,
                "Código de verificação — Savio Mock Server",
                $"<h3>Código de verificação</h3>" +
                $"<p>Seu código: <strong>{code}</strong></p>" +
                $"<p>Este código expira em 10 minutos.</p>");
        }
        catch
        {
            errorMessage = "Erro ao enviar código por e-mail. Tente novamente.";
        }
    }

    private async Task ResendEmailCode()
    {
        var twoFactorUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (twoFactorUser != null)
        {
            await SendEmailCodeAsync(twoFactorUser);
            errorMessage = null;
            StateHasChanged();
        }
    }
}
