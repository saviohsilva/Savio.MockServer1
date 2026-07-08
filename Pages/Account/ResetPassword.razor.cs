using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Pages.Account;

public partial class ResetPassword
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [SupplyParameterFromQuery]
    public string? Code { get; set; }

    private readonly ResetPasswordModel resetPasswordModel = new();
    private bool invalidLink;
    private bool passwordReset;
    private bool isLoading;
    private string? errorMessage;

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Code))
        {
            invalidLink = true;
            return;
        }

        resetPasswordModel.Email = Email;
    }

    private async Task HandleResetPassword()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                invalidLink = true;
                return;
            }

            if (resetPasswordModel.NewPassword != resetPasswordModel.ConfirmPassword)
            {
                errorMessage = "As senhas não coincidem.";
                return;
            }

            var user = await UserManager.FindByEmailAsync(resetPasswordModel.Email.Trim());
            if (user == null)
            {
                errorMessage = "Não foi possível redefinir sua senha com os dados informados.";
                return;
            }

            var result = await UserManager.ResetPasswordAsync(user, Code, resetPasswordModel.NewPassword);
            if (result.Succeeded)
            {
                passwordReset = true;
            }
            else
            {
                errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Erro ao redefinir senha: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private sealed class ResetPasswordModel
    {
        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nova senha é obrigatória")]
        [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
