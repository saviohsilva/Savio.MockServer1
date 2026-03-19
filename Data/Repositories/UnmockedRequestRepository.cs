using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public class UnmockedRequestRepository : IUnmockedRequestRepository
{
    private readonly MockDbContext _context;

    public UnmockedRequestRepository(MockDbContext context)
    {
        _context = context;
    }

    public async Task<List<UnmockedRequestEntity>> GetAllAsync(int skip = 0, int take = 50)
    {
        return await _context.UnmockedRequests
            .Where(r => !r.MockCreated)
            .OrderByDescending(r => r.LastSeenAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.UnmockedRequests
            .Where(r => !r.MockCreated)
            .CountAsync();
    }

    public async Task<UnmockedRequestEntity?> GetByIdAsync(int id)
    {
        return await _context.UnmockedRequests.FindAsync(id);
    }

    public async Task<UnmockedRequestEntity?> GetByRouteAndMethodAsync(string route, string method)
    {
        return await _context.UnmockedRequests
            .FirstOrDefaultAsync(r => r.Route == route && r.Method == method && !r.MockCreated);
    }

    public async Task<UnmockedRequestEntity> AddOrUpdateAsync(UnmockedRequestEntity entity)
    {
        var existing = await GetByRouteAndMethodAsync(entity.Route, entity.Method);
        
        if (existing != null)
        {
            // Atualizar existente
            existing.LastSeenAt = DateTime.UtcNow;
            existing.HitCount++;
            existing.LastClientIp = entity.LastClientIp;
            existing.RequestHeadersJson = entity.RequestHeadersJson;
            existing.RequestBody = entity.RequestBody;
            
            await _context.SaveChangesAsync();
            return existing;
        }
        else
        {
            // Criar novo
            _context.UnmockedRequests.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }

    public async Task MarkAsMockCreatedAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            entity.MockCreated = true;
            entity.MockCreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.UnmockedRequests.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
