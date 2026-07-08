using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public class MockCertificateRepository(MockDbContext context) : IMockCertificateRepository
{
    private readonly MockDbContext _context = context;

    public async Task<List<MockCertificateEntity>> GetAllAsync(string? userId = null)
    {
        var query = _context.MockCertificates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(c => c.UserId == userId);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<MockCertificateEntity?> GetByIdAsync(int id)
    {
        return await _context.MockCertificates.FindAsync(id);
    }

    public async Task<MockCertificateEntity?> GetByThumbprintAsync(string thumbprint)
    {
        return await _context.MockCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Thumbprint == thumbprint);
    }

    public async Task<MockCertificateEntity> AddAsync(MockCertificateEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        _context.MockCertificates.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.MockCertificates
            .Include(c => c.AuthConfigs)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity != null)
        {
            foreach (var authConfig in entity.AuthConfigs)
            {
                authConfig.RequiredCertificateId = null;
                authConfig.RequiredCertificate = null;
            }

            var endpoints = await _context.MockEndpoints
                .Where(e => e.RequiredClientCertificateId == id)
                .ToListAsync();

            foreach (var endpoint in endpoints)
            {
                endpoint.RequiredClientCertificateId = null;
                endpoint.RequiredClientCertificate = null;
                endpoint.RequireClientCertificate = false;
            }

            _context.MockCertificates.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
