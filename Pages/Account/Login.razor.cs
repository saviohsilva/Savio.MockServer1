using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Pages.Account;

public partial class Login
{
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    [SupplyParameterFromQuery]
    public string? Reset { get; set; }

    [SupplyParameterFromQuery]
    public string? CredentialUpdated { get; set; }

    [SupplyParameterFromQuery]
    public string? Resend { get; set; }

    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    private string? errorMessage;
    private string? successMessage;
    private string resendEmail = string.Empty;
    private bool showResendConfirmation;

    protected override void OnInitialized()
    {
        resendEmail = (Email ?? string.Empty).Trim();

        errorMessage = Error switch
        {
            "invalid" => "E-mail ou senha inválidos.",
            "locked" => "Conta bloqueada temporariamente. Tente novamente em alguns minutos.",
            "notallowed" => "Conta não confirmada. Verifique seu e-mail para ativar a conta.",
            _ => null
        };

        showResendConfirmation = Error == "notallowed" ||
                                 Resend is "error" or "missing";

        if (Resend == "success")
        {
            successMessage = "Se existir uma conta pendente para esse e-mail, o link de confirmação foi reenviado.";
            showResendConfirmation = false;
        }
        else if (Resend == "already-confirmed")
        {
            successMessage = "Esse e-mail já está confirmado. Você já pode fazer login normalmente.";
            showResendConfirmation = false;
        }
        else if (Resend == "error")
        {
            errorMessage = "Não foi possível reenviar o e-mail de confirmação. Tente novamente em instantes.";
        }
        else if (Resend == "missing")
        {
            errorMessage = "Informe o e-mail para reenviar a confirmação da conta.";
        }

        if (Reset == "success")
        {
            successMessage = "Senha redefinida com sucesso. Faça login com sua nova senha.";
            return;
        }

        if (CredentialUpdated == "true")
        {
            successMessage = "Senha alterada com sucesso. Faça login novamente.";
        }
    }
}
