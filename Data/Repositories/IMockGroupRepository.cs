using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public interface IMockGroupRepository
{
    Task<List<MockGroupEntity>> GetAllAsync(string? userId = null);
    Task<List<MockGroupEntity>> GetAllWithMocksAsync(string? userId = null);
    Task<MockGroupEntity?> GetByIdAsync(int id);
    Task<MockGroupEntity?> GetByIdWithMocksAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, string? userId = null);
    Task<MockGroupEntity> AddAsync(MockGroupEntity entity);
    Task UpdateAsync(MockGroupEntity entity);
    Task DeleteAsync(int id);
}
