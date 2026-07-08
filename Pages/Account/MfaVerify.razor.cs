using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Pages.Account;

public partial class MfaVerify
{
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    [SupplyParameterFromQuery]
    public string? MfaMethod { get; set; }

    [SupplyParameterFromQuery]
    public string? Resent { get; set; }

    private string? errorMessage;
    private string? infoMessage;
    private string mfaMethod = "Authenticator";

    protected override void OnInitialized()
    {
        errorMessage = Error switch
        {
            "invalid" => "Código inválido. Tente novamente.",
            "email-send" => "Erro ao enviar código por e-mail. Tente novamente.",
            _ => null
        };

        mfaMethod = string.IsNullOrWhiteSpace(MfaMethod) ? "Authenticator" : MfaMethod;

        if (Resent == "true")
            infoMessage = "Um novo código foi enviado para seu e-mail.";
    }
}
