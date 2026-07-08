using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

/// <summary>
/// Registro único (Id = 1) com as configurações de e-mail armazenadas no banco de dados.
/// A senha SMTP é armazenada criptografada via ASP.NET Core Data Protection.
/// Configurações do appsettings têm prioridade sobre este registro.
/// </summary>
public class EmailSettingEntity
{
    public int Id { get; set; }

    [MaxLength(256)]
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    [MaxLength(256)]
    public string? SmtpUser { get; set; }

    /// <summary>Senha criptografada via IDataProtector.</summary>
    public string? SmtpPassEncrypted { get; set; }

    [MaxLength(256)]
    public string? FromEmail { get; set; }

    [MaxLength(256)]
    public string? FromName { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedByUserId { get; set; }
}
