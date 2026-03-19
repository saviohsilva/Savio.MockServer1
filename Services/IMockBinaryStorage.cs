namespace Savio.MockServer.Services;

public interface IMockBinaryStorage
{
    Task<int> SaveAsync(byte[] bytes, string? contentType, string? fileName, CancellationToken cancellationToken = default);
    Task<(byte[] bytes, string contentType, string? fileName)?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
