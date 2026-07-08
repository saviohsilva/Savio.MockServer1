using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;

namespace Savio.MockServer.Data.Repositories;

public class RequestHistoryRepository(MockDbContext context) : IRequestHistoryRepository
{
    private readonly MockDbContext _context = context;

    public async Task<List<RequestHistoryEntity>> GetByMockIdAsync(int mockId, int take = 100)
    {
        return await _context.RequestHistory
            .Where(h => h.MockEndpointId == mockId)
            .OrderByDescending(h => h.RequestedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<RequestHistoryEntity>> GetRecentAsync(int skip = 0, int take = 100)
    {
        return await _context.RequestHistory
            .Include(h => h.MockEndpoint)
            .OrderByDescending(h => h.RequestedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.RequestHistory.CountAsync();
    }

    public async Task<RequestHistoryEntity> AddAsync(RequestHistoryEntity entity)
    {
        _context.RequestHistory.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteOldEntriesAsync(int keepLastDays = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-keepLastDays);
        var oldEntries = await _context.RequestHistory
            .Where(h => h.RequestedAt < cutoffDate)
            .ToListAsync();

        _context.RequestHistory.RemoveRange(oldEntries);
        await _context.SaveChangesAsync();
    }

    public async Task<List<RequestHistoryEntity>> SearchAsync(RequestHistoryFilter filter, int skip = 0, int take = 100)
    {
        var query = ApplyFilter(_context.RequestHistory.AsQueryable(), filter);

        if (filter.IncludeMockEndpoint)
        {
            query = query.Include(h => h.MockEndpoint);
        }

        return await query
            .OrderByDescending(h => h.RequestedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<RequestHistoryListItem>> SearchListAsync(RequestHistoryFilter filter, int skip = 0, int take = 100)
    {
        var query = ApplyFilter(_context.RequestHistory.AsQueryable(), filter);

        var ordered = (filter.SortColumn, filter.SortAscending) switch
        {
            ("method",    true)  => query.OrderBy(h => h.Method),
            ("method",    false) => query.OrderByDescending(h => h.Method),
            ("route",     true)  => query.OrderBy(h => h.Route),
            ("route",     false) => query.OrderByDescending(h => h.Route),
            ("status",    true)  => query.OrderBy(h => h.ResponseStatusCode),
            ("status",    false) => query.OrderByDescending(h => h.ResponseStatusCode),
            ("delay",     true)  => query.OrderBy(h => h.DelayMs),
            ("delay",     false) => query.OrderByDescending(h => h.DelayMs),
            ("ip",        true)  => query.OrderBy(h => h.ClientIp),
            ("ip",        false) => query.OrderByDescending(h => h.ClientIp),
            ("mock",      true)  => query.OrderBy(h => h.MockEndpoint != null ? h.MockEndpoint.Description : null),
            ("mock",      false) => query.OrderByDescending(h => h.MockEndpoint != null ? h.MockEndpoint.Description : null),
            _                    => filter.SortAscending
                                        ? query.OrderBy(h => h.RequestedAt)
                                        : query.OrderByDescending(h => h.RequestedAt),
        };

        return await ordered
            .Skip(skip)
            .Take(take)
            .Select(h => new RequestHistoryListItem
            {
                Id = h.Id,
                Method = h.Method,
                Route = h.Route,
                ResponseStatusCode = h.ResponseStatusCode,
                DelayMs = h.DelayMs,
                RequestedAt = h.RequestedAt,
                ClientIp = h.ClientIp,
                MockEndpointId = h.MockEndpointId,
                MockEndpointDescription = h.MockEndpoint != null ? h.MockEndpoint.Description : null
            })
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(RequestHistoryFilter filter)
    {
        var query = ApplyFilter(_context.RequestHistory.AsQueryable(), filter);
        return await query.CountAsync();
    }

    public async Task<RequestHistoryEntity?> GetByIdAsync(int id)
    {
        return await _context.RequestHistory
            .Include(h => h.MockEndpoint)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<bool> DeleteByIdAsync(int id)
    {
        var existing = await _context.RequestHistory.FirstOrDefaultAsync(h => h.Id == id);
        if (existing is null)
        {
            return false;
        }

        _context.RequestHistory.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> ClearAsync(string userId)
    {
        return await _context.RequestHistory
            .Where(h => h.MockEndpoint.UserId == userId)
            .ExecuteDeleteAsync();
    }

    private static IQueryable<RequestHistoryEntity> ApplyFilter(IQueryable<RequestHistoryEntity> query, RequestHistoryFilter filter)
    {
        if (filter.MockEndpointId.HasValue)
        {
            query = query.Where(h => h.MockEndpointId == filter.MockEndpointId.Value);
        }

        if (filter.MockGroupId.HasValue)
        {
            query = query.Where(h => h.MockEndpoint.MockGroupId == filter.MockGroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Method))
        {
            var method = filter.Method.Trim();
            query = query.Where(h => h.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(filter.RouteContains))
        {
            var route = filter.RouteContains.Trim();
            query = query.Where(h => h.Route.Contains(route));
        }

        if (filter.ResponseStatusCode.HasValue)
        {
            query = query.Where(h => h.ResponseStatusCode == filter.ResponseStatusCode.Value);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(h => h.RequestedAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(h => h.RequestedAt <= filter.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TextContains))
        {
            var txt = filter.TextContains.Trim();
            query = query.Where(h =>
                (h.Route != null && h.Route.Contains(txt)) ||
                (h.RequestBody != null && h.RequestBody.Contains(txt)) ||
                (h.ResponseBody != null && h.ResponseBody.Contains(txt)) ||
                (h.ClientIp != null && h.ClientIp.Contains(txt)));
        }

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            query = query.Where(h => h.MockEndpoint.UserId == filter.UserId);
        }

        return query;
    }
}
