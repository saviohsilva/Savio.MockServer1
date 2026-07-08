using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Savio.MockServer.Data;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Security;

namespace Savio.MockServer.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["Database:Provider"] ?? "SQLite";
        var connectionString = configuration[$"Database:ConnectionStrings:{dbProvider}"];

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException($"Connection string para o provider '{dbProvider}' não encontrada.");

        services.AddDbContext<MockDbContext>(options =>
        {
            switch (dbProvider.ToUpper())
            {
                case "SQLITE":
                    options.UseSqlite(connectionString);
                    break;
                case "MYSQL":
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    options.UseMySql(connectionString, serverVersion);
                    break;
                case "SQLSERVER":
                    options.UseSqlServer(connectionString);
                    break;
                default:
                    throw new InvalidOperationException($"Provider de banco de dados '{dbProvider}' não suportado. Use: SQLite, MySQL ou SQLServer");
            }
        });

        return services;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        var dbProvider = app.Configuration["Database:Provider"] ?? "SQLite";
        var connectionString = app.Configuration[$"Database:ConnectionStrings:{dbProvider}"];

        app.Logger.LogInformation("Configurando banco de dados: {Provider}", dbProvider);
        app.Logger.LogDebug("Connection String: {ConnectionString}",
            connectionString != null && connectionString.Length > 50
                ? connectionString[..50] + "..."
                : connectionString);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MockDbContext>();

        if (string.Equals(dbProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            app.Logger.LogInformation("Aplicando migrations (SQLite)...");
            await db.Database.MigrateAsync();

            try
            {
                await db.Database.ExecuteSqlRawAsync("SELECT ResponseBinaryBlobId FROM RequestHistory LIMIT 0");
                await db.Database.ExecuteSqlRawAsync("SELECT Id FROM AspNetUsers LIMIT 0");
                await db.Database.ExecuteSqlRawAsync("SELECT Id FROM MockAuthConfigs LIMIT 0");
                await db.Database.ExecuteSqlRawAsync("SELECT Id FROM MockCertificates LIMIT 0");
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Schema corrompido detectado. Recriando banco de dados...");
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();
                app.Logger.LogInformation("Banco de dados recriado com sucesso.");
            }
        }
        else
        {
            app.Logger.LogInformation("Criando schema via EnsureCreated ({Provider})...", dbProvider);
            await db.Database.EnsureCreatedAsync();
        }

        await EnsureMasterAdminSeedAsync(app, scope.ServiceProvider);

        app.Logger.LogInformation("Banco de dados configurado com sucesso!");
    }

    private static async Task EnsureMasterAdminSeedAsync(WebApplication app, IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<MockDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Falha ao criar role '{AppRoles.Admin}': {errors}");
            }
        }

        var seedSection = app.Configuration.GetSection("Seed:MasterUser");
        var username = seedSection["Username"] ?? "mockadmin";
        var alias = seedSection["Alias"] ?? "mockadmin";
        var password = seedSection["Password"];

        var masterUser = await userManager.FindByNameAsync(username)
            ?? await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (masterUser == null)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Configure 'Seed:MasterUser:Password' para criar o usuário master inicial.");

            alias = await BuildUniqueAliasAsync(userManager, alias);
            masterUser = new ApplicationUser
            {
                UserName = username,
                Email = null,
                EmailConfirmed = true,
                Alias = alias,
                MfaMethod = "Authenticator",
                CreatedAt = DateTime.UtcNow,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };

            masterUser.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(masterUser, password);
            masterUser.NormalizedUserName = username.ToUpperInvariant();
            masterUser.NormalizedEmail = null;

            dbContext.Users.Add(masterUser);
            await dbContext.SaveChangesAsync();

            app.Logger.LogWarning(
                "Usuário master sem e-mail criado: {Username}. Altere a senha padrão imediatamente em /account/change-password.",
                username);
        }
        else
        {
            var updateRequired = false;
            if (!masterUser.EmailConfirmed)
            {
                masterUser.EmailConfirmed = true;
                updateRequired = true;
            }

            if (string.IsNullOrWhiteSpace(masterUser.NormalizedUserName))
            {
                masterUser.NormalizedUserName = username.ToUpperInvariant();
                updateRequired = true;
            }

            if (updateRequired)
                await userManager.UpdateAsync(masterUser);
        }

        var adminRole = await roleManager.FindByNameAsync(AppRoles.Admin)
            ?? throw new InvalidOperationException($"Role '{AppRoles.Admin}' não encontrada após seed.");

        var hasUserRoleLink = await dbContext.UserRoles.AnyAsync(ur =>
            ur.UserId == masterUser.Id && ur.RoleId == adminRole.Id);

        if (!hasUserRoleLink)
        {
            dbContext.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = masterUser.Id,
                RoleId = adminRole.Id
            });
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task<string> BuildUniqueAliasAsync(UserManager<ApplicationUser> userManager, string preferredAlias)
    {
        var baseAlias = string.IsNullOrWhiteSpace(preferredAlias) ? "mockadmin" : preferredAlias.Trim().ToLowerInvariant();
        if (!await userManager.Users.AnyAsync(u => u.Alias == baseAlias))
            return baseAlias;

        var suffix = 2;
        var candidate = $"{baseAlias}{suffix}";
        while (await userManager.Users.AnyAsync(u => u.Alias == candidate))
        {
            suffix++;
            candidate = $"{baseAlias}{suffix}";
        }

        return candidate;
    }
}
