using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;

namespace Savio.MockServer.Data.Repositories;

public interface IMockRepository
{
    Task<List<MockEndpointEntity>> GetAllAsync(string? userId = null);
    Task<List<MockEndpointEntity>> GetFilteredAsync(MockFilter filter);
    Task<List<MockEndpointEntity>> GetStandaloneMocksAsync(MockFilter? filter = null);
    Task<List<MockEndpointEntity>> GetByGroupIdAsync(int groupId);
    Task<MockEndpointEntity?> GetByIdAsync(int id);
    Task<MockEndpointEntity?> GetActiveByRouteAndMethodAsync(string route, string method, int? excludeId = null, string? userId = null);
    Task<MockEndpointEntity> AddAsync(MockEndpointEntity entity);
    Task UpdateAsync(MockEndpointEntity entity);
    Task DeleteAsync(int id);
    Task IncrementCallCountAsync(int id);
    Task SetActiveBulkAsync(IEnumerable<int> ids, bool isActive);
}
