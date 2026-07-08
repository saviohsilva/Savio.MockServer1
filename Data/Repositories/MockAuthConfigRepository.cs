using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public class MockAuthConfigRepository(MockDbContext context) : IMockAuthConfigRepository
{
    private readonly MockDbContext _context = context;

    public async Task<List<MockAuthConfigEntity>> GetAllAsync(string? userId = null)
    {
        var query = _context.MockAuthConfigs
            .AsNoTracking()
            .Include(a => a.RequiredCertificate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<MockAuthConfigEntity?> GetByIdAsync(int id)
    {
        return await _context.MockAuthConfigs.FindAsync(id);
    }

    public async Task<MockAuthConfigEntity?> GetByIdWithCertificateAsync(int id)
    {
        return await _context.MockAuthConfigs
            .AsNoTracking()
            .Include(a => a.RequiredCertificate)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<MockAuthConfigEntity> AddAsync(MockAuthConfigEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        _context.MockAuthConfigs.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(MockAuthConfigEntity entity)
    {
        var tracked = _context.ChangeTracker.Entries<MockAuthConfigEntity>()
            .FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (tracked != null)
            tracked.State = EntityState.Detached;

        entity.UpdatedAt = DateTime.UtcNow;
        _context.MockAuthConfigs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.MockAuthConfigs
            .Include(a => a.MockEndpoints)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (entity != null)
        {
            foreach (var endpoint in entity.MockEndpoints)
            {
                endpoint.AuthConfigId = null;
                endpoint.AuthConfig = null;
                endpoint.AuthEndpointRole = null;
            }

            _context.MockAuthConfigs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
