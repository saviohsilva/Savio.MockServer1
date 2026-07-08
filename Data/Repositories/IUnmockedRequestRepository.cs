using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public interface IUnmockedRequestRepository
{
    Task<List<UnmockedRequestEntity>> GetAllAsync(string? userId = null, int skip = 0, int take = 50);
    Task<int> GetTotalCountAsync(string? userId = null);
    Task<UnmockedRequestEntity?> GetByIdAsync(int id);
    Task<UnmockedRequestEntity?> GetByRouteAndMethodAsync(string route, string method, string? userId = null);
    Task<UnmockedRequestEntity> AddOrUpdateAsync(UnmockedRequestEntity entity);
    Task MarkAsMockCreatedAsync(int id);
    Task DeleteAsync(int id);
}
