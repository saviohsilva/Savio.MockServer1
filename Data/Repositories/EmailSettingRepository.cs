using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Data.Repositories;

public class EmailSettingRepository(MockDbContext context) : IEmailSettingRepository
{
    private const int SingletonId = 1;

    public Task<EmailSettingEntity?> GetAsync()
        => context.EmailSettings.FirstOrDefaultAsync(e => e.Id == SingletonId);

    public async Task SaveAsync(EmailSettingEntity entity)
    {
        entity.Id = SingletonId;

        var existing = await context.EmailSettings.FindAsync(SingletonId);
        if (existing == null)
        {
            context.EmailSettings.Add(entity);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(entity);
        }

        await context.SaveChangesAsync();
    }
}
