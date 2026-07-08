using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public class MockCertificateEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Thumbprint { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>Dados binários do certificado .pfx armazenados diretamente no banco.</summary>
    public byte[] CertificateData { get; set; } = [];

    public bool HasPassword { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    // Proprietário
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Navegação reversa
    public ICollection<MockAuthConfigEntity> AuthConfigs { get; set; } = [];
}
