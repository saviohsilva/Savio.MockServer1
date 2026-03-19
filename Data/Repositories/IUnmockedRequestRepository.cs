using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public interface IUnmockedRequestRepository
{
    Task<List<UnmockedRequestEntity>> GetAllAsync(int skip = 0, int take = 50);
    Task<int> GetTotalCountAsync();
    Task<UnmockedRequestEntity?> GetByIdAsync(int id);
    Task<UnmockedRequestEntity?> GetByRouteAndMethodAsync(string route, string method);
    Task<UnmockedRequestEntity> AddOrUpdateAsync(UnmockedRequestEntity entity);
    Task MarkAsMockCreatedAsync(int id);
    Task DeleteAsync(int id);
}
