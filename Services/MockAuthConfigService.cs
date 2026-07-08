using Savio.MockServer.Data.Entities;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Models;
using System.Text.Json;

namespace Savio.MockServer.Services;

public class MockAuthConfigService(
    IMockAuthConfigRepository repository)
{
    private readonly IMockAuthConfigRepository _repository = repository;

    public async Task<List<MockAuthConfig>> GetAllAsync(string? userId = null)
    {
        var entities = await _repository.GetAllAsync(userId);
        return [.. entities.Select(EntityToModel)];
    }

    public async Task<MockAuthConfig?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdWithCertificateAsync(id);
        return entity != null ? EntityToModel(entity) : null;
    }

    public async Task<(bool success, string? error, int id)> AddAsync(MockAuthConfig model, string? userId)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return (false, "Nome é obrigatório.", 0);

        var entity = ModelToEntity(model);
        entity.UserId = userId;

        var created = await _repository.AddAsync(entity);
        return (true, null, created.Id);
    }

    public async Task<(bool success, string? error)> UpdateAsync(MockAuthConfig model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return (false, "Nome é obrigatório.");

        var existing = await _repository.GetByIdAsync(model.Id);
        if (existing == null)
            return (false, "Configuração não encontrada.");

        var entity = ModelToEntity(model);
        entity.Id = model.Id;
        entity.UserId = existing.UserId;
        entity.CreatedAt = existing.CreatedAt;

        await _repository.UpdateAsync(entity);
        return (true, null);
    }

    public async Task DeleteAsync(int id) => await _repository.DeleteAsync(id);

    // ── Mapeamento ─────────────────────────────────────────────────────────

    public static MockAuthConfig EntityToModel(MockAuthConfigEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Type = e.Type,
        Username = e.Username,
        Password = e.Password,
        UsernameParamName = e.UsernameParamName,
        UsernameParamLocation = e.UsernameParamLocation,
        PasswordParamName = e.PasswordParamName,
        PasswordParamLocation = e.PasswordParamLocation,
        CustomValidationParams = DeserializeValidationParams(e.CustomValidationParamsJson),
        CustomTokenReturnLocation = e.CustomTokenReturnLocation,
        CustomTokenReturnName = e.CustomTokenReturnName,
        CustomTokenPrefix = e.CustomTokenPrefix,
        CustomTokenSuffix = e.CustomTokenSuffix,
        GenerateJwtToken = e.GenerateJwtToken,
        JwtSecretKey = e.JwtSecretKey,
        JwtExpirationMinutes = e.JwtExpirationMinutes,
        JwtIssuer = e.JwtIssuer,
        JwtAudience = e.JwtAudience,
        JwtAdditionalClaimsJson = e.JwtAdditionalClaimsJson,
        ApiKeyHeader = e.ApiKeyHeader,
        ApiKeyValue = e.ApiKeyValue,
        RequireCertificate = e.RequireCertificate,
        RequiredCertificateId = e.RequiredCertificateId,
        RequiredCertificateName = e.RequiredCertificate?.Name,
        RequiredCertificateThumbprint = e.RequiredCertificate?.Thumbprint,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };

    private static MockAuthConfigEntity ModelToEntity(MockAuthConfig m) => new()
    {
        Name = m.Name,
        Description = m.Description,
        Type = m.Type,
        Username = m.Username,
        Password = m.Password,
        UsernameParamName = m.UsernameParamName,
        UsernameParamLocation = m.UsernameParamLocation,
        PasswordParamName = m.PasswordParamName,
        PasswordParamLocation = m.PasswordParamLocation,
        CustomValidationParamsJson = SerializeValidationParams(m.CustomValidationParams),
        CustomTokenReturnLocation = m.CustomTokenReturnLocation,
        CustomTokenReturnName = m.CustomTokenReturnName,
        CustomTokenPrefix = m.CustomTokenPrefix,
        CustomTokenSuffix = m.CustomTokenSuffix,
        GenerateJwtToken = m.GenerateJwtToken,
        JwtSecretKey = m.JwtSecretKey,
        JwtExpirationMinutes = m.JwtExpirationMinutes > 0 ? m.JwtExpirationMinutes : 60,
        JwtIssuer = m.JwtIssuer,
        JwtAudience = m.JwtAudience,
        JwtAdditionalClaimsJson = m.JwtAdditionalClaimsJson,
        ApiKeyHeader = m.ApiKeyHeader,
        ApiKeyValue = m.ApiKeyValue,
        RequireCertificate = m.RequireCertificate,
        RequiredCertificateId = m.RequiredCertificateId
    };

    private static List<AuthValidationParam> DeserializeValidationParams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<AuthValidationParam>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? SerializeValidationParams(List<AuthValidationParam>? items)
    {
        if (items == null || items.Count == 0)
        {
            return null;
        }

        var cleaned = items
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new AuthValidationParam
            {
                Name = p.Name.Trim(),
                Value = p.Value ?? string.Empty,
                Location = p.Location
            })
            .ToList();

        return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned);
    }
}
