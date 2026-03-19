using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Pages.Account;

public partial class Login
{
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string? errorMessage;

    protected override void OnInitialized()
    {
        errorMessage = Error switch
        {
            "invalid"    => "E-mail ou senha inválidos.",
            "locked"     => "Conta bloqueada temporariamente. Tente novamente em alguns minutos.",
            "notallowed" => "Conta não confirmada. Verifique seu e-mail para ativar a conta.",
            _            => null
        };
    }
}
