using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public enum MockAuthType
{
    Basic = 0,
    Bearer = 1,
    ApiKey = 2,
    CustomToken = 3
}

public enum AuthParamLocation
{
    Body = 0,
    QueryString = 1,
    Header = 2
}

public enum TokenReturnLocation
{
    Body = 0,
    Header = 1
}

/// <summary>
/// Define como este endpoint participa do fluxo de autenticação.
/// </summary>
public enum MockAuthEndpointRole
{
    /// <summary>
    /// Endpoint que emite tokens (ex: POST /api/token).
    /// Valida credenciais da requisição e retorna JWT real quando válido.
    /// </summary>
    TokenIssuer = 0,

    /// <summary>
    /// Endpoint protegido que exige autenticação válida para ser servido.
    /// </summary>
    Protected = 1
}

public class MockAuthConfigEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public MockAuthType Type { get; set; } = MockAuthType.Bearer;

    // ── Credenciais (Basic / Bearer com username+password) ──────────────

    [MaxLength(200)]
    public string? Username { get; set; }

    [MaxLength(200)]
    public string? Password { get; set; }

    [MaxLength(200)]
    public string? UsernameParamName { get; set; }

    public AuthParamLocation UsernameParamLocation { get; set; } = AuthParamLocation.Body;

    [MaxLength(200)]
    public string? PasswordParamName { get; set; }

    public AuthParamLocation PasswordParamLocation { get; set; } = AuthParamLocation.Body;

    /// <summary>JSON de parâmetros de validação customizados, ex: [{"name":"usuario","value":"admin","location":1}]</summary>
    public string? CustomValidationParamsJson { get; set; }

    public TokenReturnLocation CustomTokenReturnLocation { get; set; } = TokenReturnLocation.Body;

    [MaxLength(200)]
    public string? CustomTokenReturnName { get; set; }

    [MaxLength(200)]
    public string? CustomTokenPrefix { get; set; }

    [MaxLength(200)]
    public string? CustomTokenSuffix { get; set; }

    // ── JWT (Bearer) ─────────────────────────────────────────────────────

    public bool GenerateJwtToken { get; set; } = true;

    [MaxLength(500)]
    public string? JwtSecretKey { get; set; }

    public int JwtExpirationMinutes { get; set; } = 60;

    [MaxLength(200)]
    public string? JwtIssuer { get; set; }

    [MaxLength(200)]
    public string? JwtAudience { get; set; }

    /// <summary>JSON com claims adicionais a incluir no token, ex: {"role":"admin"}.</summary>
    public string? JwtAdditionalClaimsJson { get; set; }

    // ── API Key ───────────────────────────────────────────────────────────

    /// <summary>Nome do header onde a chave é enviada (ex: X-API-Key).</summary>
    [MaxLength(200)]
    public string? ApiKeyHeader { get; set; }

    [MaxLength(500)]
    public string? ApiKeyValue { get; set; }

    // ── Certificado de cliente (mTLS) ─────────────────────────────────────

    public bool RequireCertificate { get; set; }

    /// <summary>ID do certificado gerado internamente cujo thumbprint será validado.</summary>
    public int? RequiredCertificateId { get; set; }
    public MockCertificateEntity? RequiredCertificate { get; set; }

    // ── Propriedade / auditoria ───────────────────────────────────────────

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navegação reversa
    public ICollection<MockEndpointEntity> MockEndpoints { get; set; } = [];
}
