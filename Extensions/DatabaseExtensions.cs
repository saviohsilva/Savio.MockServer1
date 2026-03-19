using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Savio.MockServer.Data;

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
        app.Logger.LogInformation("Connection String: {ConnectionString}",
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
            }
            catch
            {
                app.Logger.LogWarning("Schema corrompido detectado. Recriando banco de dados...");
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

        app.Logger.LogInformation("Banco de dados configurado com sucesso!");
    }
}
