using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data;
using Savio.MockServer.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Savio.MockServer.Pages.Account;

public partial class Register
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IEmailSender EmailSender { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private MockDbContext DbContext { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private RegisterModel registerModel = new();
    private string? errorMessage;
    private bool isLoading;

    private string BaseDisplayUrl => Navigation.BaseUri.TrimEnd('/');

    private async Task HandleRegister()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            if (registerModel.Password != registerModel.ConfirmPassword)
            {
                errorMessage = "As senhas não coincidem.";
                return;
            }

            var alias = registerModel.Alias.Trim().ToLowerInvariant();

            if (!Regex.IsMatch(alias, @"^[a-z0-9][a-z0-9_-]{1,48}[a-z0-9]$"))
            {
                errorMessage = "O alias deve conter entre 3 e 50 caracteres, apenas letras minúsculas, números, hífens e underscores. Deve começar e terminar com letra ou número.";
                return;
            }

            var aliasExists = await DbContext.Users.AnyAsync(u => u.Alias == alias);
            if (aliasExists)
            {
                errorMessage = $"O alias '{alias}' já está em uso. Escolha outro.";
                return;
            }

            var user = new ApplicationUser
            {
                UserName = registerModel.Email,
                Email = registerModel.Email,
                Alias = alias
            };

            var result = await UserManager.CreateAsync(user, registerModel.Password);

            if (result.Succeeded)
            {
                var smtpHost = Configuration["Email:SmtpHost"];
                var smtpConfigured = !string.IsNullOrWhiteSpace(smtpHost);

                if (smtpConfigured)
                {
                    var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = $"{Navigation.BaseUri}account/confirm-email?userId={user.Id}&code={Uri.EscapeDataString(code)}";

                    await EmailSender.SendEmailAsync(
                        registerModel.Email,
                        "Confirme sua conta — Savio Mock Server",
                        $"<h3>Bem-vindo ao Savio Mock Server!</h3>" +
                        $"<p>Seu alias: <strong>{alias}</strong></p>" +
                        $"<p>Clique no link abaixo para confirmar sua conta:</p>" +
                        $"<p><a href='{callbackUrl}'>Confirmar Conta</a></p>");

                    Navigation.NavigateTo($"/account/register-confirmation?email={Uri.EscapeDataString(registerModel.Email)}");
                }
                else
                {
                    var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                    await UserManager.ConfirmEmailAsync(user, token);
                    Navigation.NavigateTo($"/account/register-confirmation?email={Uri.EscapeDataString(registerModel.Email)}&autoConfirmed=true");
                }
            }
            else
            {
                errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Erro ao registrar: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private sealed class RegisterModel
    {
        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alias é obrigatório")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Alias deve ter entre 3 e 50 caracteres")]
        public string Alias { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
