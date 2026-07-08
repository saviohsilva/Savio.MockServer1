using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class Certificados
{
    private const string DownloadBase64File = "downloadBase64File";
    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] public IModalService Modal { get; set; } = default!;

    private List<MockCertificate> certificates = [];
    private bool isLoading = true;
    private MockCertificate? certToDelete;
    private string? alertMessage;
    private string alertClass = "alert-success";
    private string alertIcon = "bi-check-circle";
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var auth = await AuthState;
            var user = await UserManager.GetUserAsync(auth.User);
            currentUserId = user?.Id;
        }

        await LoadCertificatesAsync();
    }

    private async Task LoadCertificatesAsync()
    {
        isLoading = true;
        try
        {
            certificates = await CertificateService.GetAllAsync(currentUserId);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task DownloadPfx(int id)
    {
        var result = await CertificateService.GetDownloadAsync(id);
        if (result == null)
        {
            ShowAlert("Arquivo do certificado não encontrado.", false);
            return;
        }

        var base64 = Convert.ToBase64String(result.Value.bytes);
        await JS.InvokeVoidAsync(DownloadBase64File, base64, result.Value.fileName, "application/x-pkcs12");
    }

    private async Task DownloadCer(int id)
    {
        var cert = await CertificateService.GetByIdAsync(id);
        if (cert == null) return;

        string? password = null;
        if (cert.HasPassword)
        {
            var parameters = new ModalParameters
            {
                { "Message", "Digite a senha do certificado para exportar o .cer:" },
                { "Icon", "bi-file-earmark-lock2" },
                { "IconColor", "primary" },
                { "Placeholder", "Senha do certificado" }
            };
            var options = new ModalOptions { Size = ModalSize.Small };
            var inputModal = Modal.Show<Savio.MockServer.Components.InputDialog>("Exportar .cer", parameters, options);
            var inputResult = await inputModal.Result;

            if (inputResult.Cancelled) return;
            password = inputResult.Data as string ?? "";
        }

        var result = await CertificateService.GetCerDownloadAsync(id, password);
        if (result == null)
        {
            ShowAlert("Não foi possível exportar o .cer. Verifique a senha.", false);
            return;
        }

        var base64 = Convert.ToBase64String(result.Value.bytes);
        await JS.InvokeVoidAsync(DownloadBase64File, base64, result.Value.fileName, "application/x-x509-ca-cert");
    }

    private async Task DownloadPemAndKey(int id)
    {
        var cert = await CertificateService.GetByIdAsync(id);
        if (cert == null) return;

        string? password = null;
        if (cert.HasPassword)
        {
            var parameters = new ModalParameters
            {
                { "Message", "Digite a senha do certificado para exportar .pem/.key:" },
                { "Icon", "bi-file-earmark-lock2" },
                { "IconColor", "primary" },
                { "Placeholder", "Senha do certificado" }
            };
            var options = new ModalOptions { Size = ModalSize.Small };
            var inputModal = Modal.Show<Savio.MockServer.Components.InputDialog>("Exportar .pem/.key", parameters, options);
            var inputResult = await inputModal.Result;

            if (inputResult.Cancelled) return;
            password = inputResult.Data as string ?? "";
        }

        var result = await CertificateService.GetPemAndKeyDownloadAsync(id, password);
        if (result == null)
        {
            ShowAlert("Não foi possível exportar .pem/.key. Verifique a senha. Se o certificado for antigo, gere um novo certificado.", false);
            return;
        }

        var certBase64 = Convert.ToBase64String(result.Value.certBytes);
        await JS.InvokeVoidAsync(DownloadBase64File, certBase64, result.Value.certFileName, "application/x-pem-file");

        var keyBase64 = Convert.ToBase64String(result.Value.keyBytes);
        await JS.InvokeVoidAsync(DownloadBase64File, keyBase64, result.Value.keyFileName, "application/x-pem-file");
    }

    private async Task CopyThumbprint(string thumbprint)
    {
        await JS.InvokeVoidAsync("copyToClipboard", thumbprint);
    }

    private void ConfirmDelete(MockCertificate cert)
    {
        certToDelete = cert;
    }

    private async Task ExecuteDelete()
    {
        if (certToDelete == null) return;

        await CertificateService.DeleteAsync(certToDelete.Id);
        certToDelete = null;
        ShowAlert("Certificado removido com sucesso.", true);
        await LoadCertificatesAsync();
    }

    private void ShowAlert(string message, bool success)
    {
        alertMessage = message;
        alertClass = success ? "alert-success" : "alert-danger";
        alertIcon = success ? "bi-check-circle" : "bi-exclamation-triangle";
    }

    private static bool IsExpired(MockCertificate cert) => cert.ExpiresAt < DateTime.UtcNow;

    private static bool IsExpiringSoon(MockCertificate cert)
        => !IsExpired(cert) && cert.ExpiresAt < DateTime.UtcNow.AddDays(30);
}
