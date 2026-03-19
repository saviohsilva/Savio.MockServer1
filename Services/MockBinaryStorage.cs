using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Services;

public sealed class MockBinaryStorage : IMockBinaryStorage
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly MockDbContext _db;

    public MockBinaryStorage(MockDbContext db)
    {
        _db = db;
    }

    public async Task<int> SaveAsync(byte[] bytes, string? contentType, string? fileName, CancellationToken cancellationToken = default)
    {
        var entity = new MockBinaryBlobEntity
        {
            Bytes = bytes ?? Array.Empty<byte>(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType,
            FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName,
            CreatedAt = DateTime.UtcNow
        };

        _db.MockBinaryBlobs.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<(byte[] bytes, string contentType, string? fileName)?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.MockBinaryBlobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return (entity.Bytes ?? Array.Empty<byte>(), entity.ContentType ?? DefaultContentType, entity.FileName);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var affected = await _db.MockBinaryBlobs
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affected > 0;
    }
}
