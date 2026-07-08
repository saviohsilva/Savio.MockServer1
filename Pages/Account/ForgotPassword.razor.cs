using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Savio.MockServer.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Pages.Account;

public partial class ForgotPassword
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IEmailSender EmailSender { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly ForgotPasswordModel forgotPasswordModel = new();
    private bool emailSent;
    private bool isLoading;
    private string? errorMessage;

    private async Task HandleForgotPassword()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var email = forgotPasswordModel.Email.Trim();
            var user = await UserManager.FindByEmailAsync(email);

            if (user != null && await UserManager.IsEmailConfirmedAsync(user))
            {
                var code = await UserManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = $"{Navigation.BaseUri}account/reset-password?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(code)}";

                await EmailSender.SendEmailAsync(
                    email,
                    "Redefinição de senha — Savio Mock Server",
                    $"<h3>Redefinição de senha</h3>" +
                    "<p>Recebemos uma solicitação para redefinir sua senha.</p>" +
                    $"<p><a href='{callbackUrl}'>Clique aqui para redefinir sua senha</a></p>" +
                    "<p>Se você não solicitou essa ação, ignore este e-mail.</p>");
            }

            emailSent = true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Erro ao processar solicitação: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private sealed class ForgotPasswordModel
    {
        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; } = string.Empty;
    }
}
