using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public class MockGroupRepository(MockDbContext context) : IMockGroupRepository
{
    private readonly MockDbContext _context = context;

    public async Task<List<MockGroupEntity>> GetAllAsync(string? userId = null)
    {
        var query = _context.MockGroups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(g => g.UserId == userId);

        return await query.OrderBy(g => g.Name).ToListAsync();
    }

    public async Task<List<MockGroupEntity>> GetAllWithMocksAsync(string? userId = null)
    {
        var query = _context.MockGroups
            .AsNoTracking()
            .Include(g => g.MockEndpoints)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(g => g.UserId == userId);

        return await query.OrderBy(g => g.Name).ToListAsync();
    }

    public async Task<MockGroupEntity?> GetByIdAsync(int id)
    {
        return await _context.MockGroups.FindAsync(id);
    }

    public async Task<MockGroupEntity?> GetByIdWithMocksAsync(int id)
    {
        return await _context.MockGroups
            .AsNoTracking()
            .Include(g => g.MockEndpoints)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, string? userId = null)
    {
        var query = _context.MockGroups.Where(g => g.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(g => g.UserId == userId);
        }

        return await query.AnyAsync();
    }

    public async Task<MockGroupEntity> AddAsync(MockGroupEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        _context.MockGroups.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(MockGroupEntity entity)
    {
        var existingEntry = _context.ChangeTracker.Entries<MockGroupEntity>()
            .FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (existingEntry != null)
        {
            existingEntry.State = EntityState.Detached;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        _context.MockGroups.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.MockGroups
            .Include(g => g.MockEndpoints)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (entity != null)
        {
            foreach (var mock in entity.MockEndpoints)
            {
                mock.MockGroupId = null;
            }

            _context.MockGroups.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
