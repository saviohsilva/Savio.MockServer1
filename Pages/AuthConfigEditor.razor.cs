using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class AuthConfigEditor
{
    [Parameter]
    public int? Id { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private MockAuthConfig model = new()
    {
        Type = MockAuthType.Bearer,
        GenerateJwtToken = true,
        JwtExpirationMinutes = 60,
        UsernameParamName = "username",
        PasswordParamName = "password",
        UsernameParamLocation = AuthParamLocation.Body,
        PasswordParamLocation = AuthParamLocation.Body,
        CustomTokenReturnLocation = TokenReturnLocation.Body,
        CustomTokenReturnName = "token"
    };

    private List<MockCertificate> certificates = [];
    private bool isSaving;
    private string? saveError;
    private string? currentUserId;

    private bool IsEdit => Id.HasValue;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var auth = await AuthState;
            var user = await UserManager.GetUserAsync(auth.User);
            currentUserId = user?.Id;
        }

        certificates = await CertificateService.GetAllAsync(currentUserId);

        if (IsEdit)
        {
            var existing = await AuthConfigService.GetByIdAsync(Id!.Value);
            if (existing != null)
                model = existing;
        }
    }

    private void OnTypeChanged()
    {
        // Reset fields that don't apply to the new type
        if (model.Type != MockAuthType.Bearer && model.Type != MockAuthType.CustomToken)
            model.GenerateJwtToken = false;
        if (model.Type != MockAuthType.ApiKey)
        {
            model.ApiKeyHeader = null;
            model.ApiKeyValue = null;
        }

        if (model.Type == MockAuthType.CustomToken)
        {
            model.GenerateJwtToken = true;
            model.CustomTokenReturnName ??= "token";
        }
    }

    private void AddCustomValidationParam()
    {
        model.CustomValidationParams.Add(new AuthValidationParam());
    }

    private void RemoveCustomValidationParam(AuthValidationParam param)
    {
        model.CustomValidationParams.Remove(param);
    }

    private async Task Save()
    {
        saveError = null;
        isSaving = true;

        model.CustomValidationParams = [.. model.CustomValidationParams
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new AuthValidationParam
            {
                Name = p.Name.Trim(),
                Value = p.Value ?? string.Empty,
                Location = p.Location
            })];

        if (model.Type == MockAuthType.CustomToken && model.CustomValidationParams.Count == 0)
        {
            saveError = "Adicione pelo menos um parâmetro de validação para o tipo Token Customizado.";
            isSaving = false;
            return;
        }

        try
        {
            bool success;
            string? error;
            if (IsEdit)
            {
                (success, error) = await AuthConfigService.UpdateAsync(model);
            }
            else
            {
                (success, error, _) = await AuthConfigService.AddAsync(model, currentUserId);
            }

            if (!success)
            {
                saveError = error;
                return;
            }

            Navigation.NavigateTo("/auth-configs");
        }
        catch (Exception ex)
        {
            saveError = $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }
}
