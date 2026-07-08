using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class CertificadoEditor
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private string name = string.Empty;
    private string? password;
    private bool showPassword;
    private bool isGenerating;
    private string? nameError;
    private string? errorMessage;
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var auth = await AuthState;
            var user = await UserManager.GetUserAsync(auth.User);
            currentUserId = user?.Id;
        }
    }

    private void TogglePasswordVisibility() => showPassword = !showPassword;

    private async Task Generate()
    {
        nameError = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            nameError = "Nome é obrigatório.";
            return;
        }

        isGenerating = true;
        try
        {
            await CertificateService.GenerateAsync(name.Trim(), password, currentUserId);
            Navigation.NavigateTo("/certificados");
        }
        catch (Exception ex)
        {
            errorMessage = $"Erro ao gerar certificado: {ex.Message}";
        }
        finally
        {
            isGenerating = false;
        }
    }
}
