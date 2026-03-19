using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;

namespace Savio.MockServer.Data.Repositories;

public class MockRepository : IMockRepository
{
    private readonly MockDbContext _context;

    public MockRepository(MockDbContext context)
    {
        _context = context;
    }

    public async Task<List<MockEndpointEntity>> GetAllAsync(string? userId = null)
    {
        var query = _context.MockEndpoints
            .Include(m => m.MockGroup)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(m => m.UserId == userId);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<List<MockEndpointEntity>> GetFilteredAsync(MockFilter filter)
    {
        var query = ApplyFilter(_context.MockEndpoints.Include(m => m.MockGroup).AsQueryable(), filter);
        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<List<MockEndpointEntity>> GetStandaloneMocksAsync(MockFilter? filter = null)
    {
        var query = _context.MockEndpoints
            .Where(m => m.MockGroupId == null)
            .AsQueryable();

        if (filter != null)
        {
            query = ApplyFilter(query, filter);
        }

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<List<MockEndpointEntity>> GetByGroupIdAsync(int groupId)
    {
        return await _context.MockEndpoints
            .Where(m => m.MockGroupId == groupId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<MockEndpointEntity?> GetByIdAsync(int id)
    {
        return await _context.MockEndpoints.FindAsync(id);
    }

    public async Task<MockEndpointEntity?> GetActiveByRouteAndMethodAsync(string route, string method, int? excludeId = null, string? userId = null)
    {
        var query = _context.MockEndpoints
            .Where(m => m.Route == route && m.Method == method && m.IsActive);

        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(m => m.UserId == userId);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<MockEndpointEntity> AddAsync(MockEndpointEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        _context.MockEndpoints.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(MockEndpointEntity entity)
    {
        var existingEntry = _context.ChangeTracker.Entries<MockEndpointEntity>()
            .FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (existingEntry != null)
        {
            existingEntry.State = EntityState.Detached;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        _context.MockEndpoints.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.MockEndpoints.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementCallCountAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            entity.CallCount++;
            entity.LastCalledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetActiveBulkAsync(IEnumerable<int> ids, bool isActive)
    {
        var idList = ids.ToList();
        await _context.MockEndpoints
            .Where(m => idList.Contains(m.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsActive, isActive)
                .SetProperty(m => m.UpdatedAt, DateTime.UtcNow));

        _context.ChangeTracker.Clear();
    }

    private static IQueryable<MockEndpointEntity> ApplyFilter(IQueryable<MockEndpointEntity> query, MockFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.UserId))
        {
            query = query.Where(m => m.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Method))
        {
            var method = filter.Method.Trim();
            query = query.Where(m => m.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(filter.RouteContains))
        {
            var route = filter.RouteContains.Trim();
            query = query.Where(m => m.Route.Contains(route));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(m => m.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.DescriptionContains))
        {
            var desc = filter.DescriptionContains.Trim();
            query = query.Where(m =>
                (m.Description != null && m.Description.Contains(desc)) ||
                m.Route.Contains(desc));
        }

        if (filter.MockGroupId.HasValue)
        {
            if (filter.MockGroupId.Value == -1)
            {
                query = query.Where(m => m.MockGroupId == null);
            }
            else
            {
                query = query.Where(m => m.MockGroupId == filter.MockGroupId.Value);
            }
        }

        return query;
    }
}
