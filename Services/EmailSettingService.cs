using Microsoft.AspNetCore.DataProtection;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Data.Repositories;

namespace Savio.MockServer.Services;

/// <summary>
/// Fornece acesso às configurações de e-mail com a seguinte ordem de prioridade:
/// 1. appsettings (Email:SmtpHost, etc.) — maior prioridade
/// 2. Banco de dados (EmailSettings) — fallback
///
/// A senha armazenada no banco é criptografada via ASP.NET Core Data Protection.
/// </summary>
public class EmailSettingService(
    IEmailSettingRepository repository,
    IConfiguration configuration,
    IDataProtectionProvider dataProtectionProvider)
{
    private const string ProtectorPurpose = "EmailSettings.SmtpPass.v1";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public record SmtpSettings(
        string SmtpHost,
        int SmtpPort,
        string? SmtpUser,
        string? SmtpPass,
        string? FromEmail,
        string? FromName);

    /// <summary>
    /// Retorna as configurações SMTP efetivas.
    /// Retorna null se nenhuma fonte estiver configurada.
    /// </summary>
    public async Task<SmtpSettings?> GetEffectiveSettingsAsync()
    {
        // appsettings tem prioridade máxima
        var appsmtpHost = configuration["Email:SmtpHost"];
        if (!string.IsNullOrWhiteSpace(appsmtpHost))
        {
            return new SmtpSettings(
                appsmtpHost,
                int.TryParse(configuration["Email:SmtpPort"], out var p) ? p : 587,
                configuration["Email:SmtpUser"],
                configuration["Email:SmtpPass"],
                configuration["Email:FromEmail"],
                configuration["Email:FromName"]);
        }

        // fallback: banco de dados
        var entity = await repository.GetAsync();
        if (entity == null || string.IsNullOrWhiteSpace(entity.SmtpHost))
            return null;

        string? plainPassword = null;
        if (!string.IsNullOrEmpty(entity.SmtpPassEncrypted))
        {
            try { plainPassword = _protector.Unprotect(entity.SmtpPassEncrypted); }
            catch { /* senha corrompida — ignorar */ }
        }

        return new SmtpSettings(
            entity.SmtpHost,
            entity.SmtpPort,
            entity.SmtpUser,
            plainPassword,
            entity.FromEmail,
            entity.FromName);
    }

    /// <summary>
    /// Indica se o e-mail está configurado em qualquer fonte.
    /// </summary>
    public async Task<bool> IsEmailConfiguredAsync()
    {
        if (!string.IsNullOrWhiteSpace(configuration["Email:SmtpHost"]))
            return true;

        var entity = await repository.GetAsync();
        return entity != null && !string.IsNullOrWhiteSpace(entity.SmtpHost);
    }

    /// <summary>
    /// Indica se a configuração ativa vem do appsettings (e, portanto, não pode ser sobrescrita pelo banco).
    /// </summary>
    public bool IsAppsettingsActive => !string.IsNullOrWhiteSpace(configuration["Email:SmtpHost"]);

    /// <summary>
    /// Retorna o registro do banco para exibição/edição na UI.
    /// Retorna a senha em texto simples apenas para pré-preenchimento — o campo senha na UI deve exibir placeholder.
    /// </summary>
    public async Task<EmailSettingEntity?> GetDbSettingsAsync() => await repository.GetAsync();

    /// <summary>
    /// Persiste as configurações de e-mail no banco com a senha criptografada.
    /// Passe <paramref name="smtpPassPlain"/> como null para manter a senha existente inalterada.
    /// Passe string vazia para limpar a senha.
    /// </summary>
    public async Task SaveAsync(
        string? smtpHost,
        int smtpPort,
        string? smtpUser,
        string? smtpPassPlain,
        string? fromEmail,
        string? fromName,
        string updatedByUserId)
    {
        var entity = await repository.GetAsync() ?? new EmailSettingEntity();

        entity.SmtpHost = smtpHost?.Trim();
        entity.SmtpPort = smtpPort;
        entity.SmtpUser = smtpUser?.Trim();

        if (smtpPassPlain != null) // null = manter senha atual; "" = limpar
        {
            entity.SmtpPassEncrypted = string.IsNullOrEmpty(smtpPassPlain)
                ? null
                : _protector.Protect(smtpPassPlain);
        }

        entity.FromEmail = fromEmail?.Trim();
        entity.FromName = fromName?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = updatedByUserId;

        await repository.SaveAsync(entity);
    }
}
