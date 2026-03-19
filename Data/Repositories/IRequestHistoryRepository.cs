using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;

namespace Savio.MockServer.Data.Repositories;

public interface IRequestHistoryRepository
{
    Task<List<RequestHistoryEntity>> GetByMockIdAsync(int mockId, int take = 100);
    Task<List<RequestHistoryEntity>> GetRecentAsync(int skip = 0, int take = 100);
    Task<int> GetTotalCountAsync();
    Task<RequestHistoryEntity> AddAsync(RequestHistoryEntity entity);
    Task DeleteOldEntriesAsync(int keepLastDays = 30);

    Task<List<RequestHistoryEntity>> SearchAsync(RequestHistoryFilter filter, int skip = 0, int take = 100);
    Task<int> GetFilteredCountAsync(RequestHistoryFilter filter);
    Task<RequestHistoryEntity?> GetByIdAsync(int id);
    Task<bool> DeleteByIdAsync(int id);
    Task<int> ClearAsync();
}
