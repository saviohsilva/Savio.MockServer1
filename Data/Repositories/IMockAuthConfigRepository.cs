using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public interface IMockAuthConfigRepository
{
    Task<List<MockAuthConfigEntity>> GetAllAsync(string? userId = null);
    Task<MockAuthConfigEntity?> GetByIdAsync(int id);
    Task<MockAuthConfigEntity?> GetByIdWithCertificateAsync(int id);
    Task<MockAuthConfigEntity> AddAsync(MockAuthConfigEntity entity);
    Task UpdateAsync(MockAuthConfigEntity entity);
    Task DeleteAsync(int id);
}
