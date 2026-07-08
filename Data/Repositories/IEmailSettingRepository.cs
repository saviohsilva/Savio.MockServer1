using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public interface IEmailSettingRepository
{
    Task<EmailSettingEntity?> GetAsync();
    Task SaveAsync(EmailSettingEntity entity);
}
