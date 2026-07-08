using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Models;

public class MockAuthConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MockAuthType Type { get; set; } = MockAuthType.Bearer;

    // Credenciais
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? UsernameParamName { get; set; }
    public AuthParamLocation UsernameParamLocation { get; set; } = AuthParamLocation.Body;
    public string? PasswordParamName { get; set; }
    public AuthParamLocation PasswordParamLocation { get; set; } = AuthParamLocation.Body;
    public List<AuthValidationParam> CustomValidationParams { get; set; } = [];

    public TokenReturnLocation CustomTokenReturnLocation { get; set; } = TokenReturnLocation.Body;
    public string? CustomTokenReturnName { get; set; }
    public string? CustomTokenPrefix { get; set; }
    public string? CustomTokenSuffix { get; set; }

    // JWT
    public bool GenerateJwtToken { get; set; } = true;
    public string? JwtSecretKey { get; set; }
    public int JwtExpirationMinutes { get; set; } = 60;
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
    public string? JwtAdditionalClaimsJson { get; set; }

    // API Key
    public string? ApiKeyHeader { get; set; }
    public string? ApiKeyValue { get; set; }

    // Certificado
    public bool RequireCertificate { get; set; }
    public int? RequiredCertificateId { get; set; }
    public string? RequiredCertificateName { get; set; }
    public string? RequiredCertificateThumbprint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class AuthValidationParam
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public AuthParamLocation Location { get; set; } = AuthParamLocation.Body;
}
