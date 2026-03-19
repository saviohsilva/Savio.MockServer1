using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Pages.Account;

public partial class ConfirmEmail
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? UserId { get; set; }

    [SupplyParameterFromQuery]
    public string? Code { get; set; }

    private bool confirmed;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(Code))
        {
            errorMessage = "Link de confirmação inválido.";
            return;
        }

        var user = await UserManager.FindByIdAsync(UserId);
        if (user == null)
        {
            errorMessage = "Usuário não encontrado.";
            return;
        }

        var result = await UserManager.ConfirmEmailAsync(user, Code);
        if (result.Succeeded)
            confirmed = true;
        else
            errorMessage = "Não foi possível confirmar o e-mail. O link pode ter expirado.";
    }
}
