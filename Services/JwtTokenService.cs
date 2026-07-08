using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Services;

/// <summary>
/// Gera e valida tokens JWT reais para uso nos mocks de autenticação.
/// </summary>
public static class JwtTokenService
{
    /// <summary>
    /// Gera um JWT assinado com as configurações do <see cref="MockAuthConfigEntity"/>.
    /// </summary>
    public static string GenerateToken(MockAuthConfigEntity config, string? subject = null)
    {
        var secret = string.IsNullOrWhiteSpace(config.JwtSecretKey)
            ? GenerateFallbackSecret(config.Id)
            : config.JwtSecretKey;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject ?? config.Username ?? "mock-user"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(config.JwtAdditionalClaimsJson))
        {
            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(config.JwtAdditionalClaimsJson);
                if (extra != null)
                {
                    foreach (var (k, v) in extra)
                        claims.Add(new Claim(k, v.ToString()));
                }
            }
            catch { /* ignora JSON inválido */ }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(config.JwtExpirationMinutes > 0 ? config.JwtExpirationMinutes : 60),
            Issuer = string.IsNullOrWhiteSpace(config.JwtIssuer) ? "Savio.MockServer" : config.JwtIssuer,
            Audience = string.IsNullOrWhiteSpace(config.JwtAudience) ? null : config.JwtAudience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    /// <summary>
    /// Valida um Bearer token contra as configurações da auth config.
    /// Retorna true se o token for válido.
    /// </summary>
    public static bool ValidateToken(MockAuthConfigEntity config, string token)
    {
        var secret = string.IsNullOrWhiteSpace(config.JwtSecretKey)
            ? GenerateFallbackSecret(config.Id)
            : config.JwtSecretKey;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = !string.IsNullOrWhiteSpace(config.JwtIssuer),
            ValidIssuer = config.JwtIssuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(config.JwtAudience),
            ValidAudience = config.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, parameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateFallbackSecret(int configId)
        => $"mock-server-fallback-secret-{configId}-!@#$%^&*()";
}
