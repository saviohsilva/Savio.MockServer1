using System.Text;
using Savio.MockServer.Endpoints;
using Savio.MockServer.Extensions;
using Savio.MockServer.Middleware;
using Savio.MockServer.Services;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplicationIdentity();
builder.Services.AddApplicationServices(builder.Environment);

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseCors();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<MockEndpointMiddleware>();
app.UseMiddleware<RequestHistoryMiddleware>();

app.MapGet("/_health", async (HttpContext context) =>
{
    using var scope = context.RequestServices.CreateScope();
    var mockService = scope.ServiceProvider.GetRequiredService<MockService>();
    var mocks = await mockService.GetAllMocksAsync();
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow, mocksCount = mocks.Count });
});

using (var scope = app.Services.CreateScope())
{
    var mockService = scope.ServiceProvider.GetRequiredService<MockService>();
    var mocks = await mockService.GetAllMocksAsync();

    app.Logger.LogInformation("=".PadRight(60, '='));
    app.Logger.LogInformation("\ud83d\ude80 Savio Mock Server iniciado!");
    app.Logger.LogInformation("\ud83d\udcca Total de mocks carregados: {Count}", mocks.Count);

    foreach (var mock in mocks.Where(m => m.IsActive))
        app.Logger.LogInformation("  \u2705 {Method} {Route} -> {StatusCode}", mock.Method, mock.Route, mock.StatusCode);

    app.Logger.LogInformation("=".PadRight(60, '='));
}

var smtpHost = app.Configuration["Email:SmtpHost"];
if (string.IsNullOrWhiteSpace(smtpHost))
    app.Logger.LogWarning("\u26a0\ufe0f SMTP n\u00e3o configurado. E-mails de confirma\u00e7\u00e3o ser\u00e3o logados no console e contas ser\u00e3o auto-confirmadas.");

app.MapAuthEndpoints();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
