using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;

namespace Savio.MockServer.Data.Repositories;

public class RequestHistoryRepository : IRequestHistoryRepository
{
    private readonly MockDbContext _context;

    public RequestHistoryRepository(MockDbContext context)
    {
        _context = context;
    }

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
