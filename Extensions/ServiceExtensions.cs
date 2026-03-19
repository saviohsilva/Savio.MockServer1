using Microsoft.AspNetCore.Identity.UI.Services;
using Blazored.Modal;
using Blazored.Toast;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Services;

namespace Savio.MockServer.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IWebHostEnvironment env)
    {
        // Repositories
        services.AddScoped<IMockRepository, MockRepository>();
        services.AddScoped<IRequestHistoryRepository, RequestHistoryRepository>();
        services.AddScoped<IUnmockedRequestRepository, UnmockedRequestRepository>();
        services.AddScoped<IMockGroupRepository, MockGroupRepository>();

        // Services
        services.AddScoped<MockService>();
        services.AddScoped<IMockBinaryStorage, MockBinaryStorage>();
        services.AddScoped<AliasService>();
        services.AddScoped<BrowserTimezoneService>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        // Blazor
        services.AddRazorPages();
        services.AddServerSideBlazor(options =>
        {
            options.MaxBufferedUnacknowledgedRenderBatches = 10;
            options.DetailedErrors = env.IsDevelopment();
        })
        .AddHubOptions(options =>
        {
            options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
            options.EnableDetailedErrors = true;
        });

        services.AddBlazoredModal();
        services.AddBlazoredToast();

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }
}
