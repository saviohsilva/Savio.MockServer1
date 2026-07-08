using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public interface IMockCertificateRepository
{
    Task<List<MockCertificateEntity>> GetAllAsync(string? userId = null);
    Task<MockCertificateEntity?> GetByIdAsync(int id);
    Task<MockCertificateEntity?> GetByThumbprintAsync(string thumbprint);
    Task<MockCertificateEntity> AddAsync(MockCertificateEntity entity);
    Task DeleteAsync(int id);
}
