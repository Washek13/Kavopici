using System.Diagnostics;
using Kavopici.Data;
using Kavopici.Services;
using Kavopici.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Settings (DB path stored in %APPDATA%/Kavopici/settings.json)
var settingsService = new AppSettingsService();
settingsService.Load();
builder.Services.AddSingleton<IAppSettingsService>(settingsService);
builder.Services.AddSingleton<IDbContextFactory<KavopiciDbContext>, KavopiciDbContextFactory>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Core services
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IBlendService, BlendService>();
builder.Services.AddTransient<ISessionService, SessionService>();
builder.Services.AddTransient<IRatingService, RatingService>();
builder.Services.AddTransient<IStatisticsService, StatisticsService>();
builder.Services.AddTransient<ICsvExportService, CsvExportService>();

// Web-specific services (scoped per circuit/session)
builder.Services.AddScoped<AppState>();

var app = builder.Build();

// Apply migrations on startup (only if a database is already configured)
if (!string.IsNullOrEmpty(settingsService.DatabasePath) && File.Exists(settingsService.DatabasePath))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KavopiciDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.Migrate();
    }
    catch
    {
        // Migration errors will be handled when Login page tries to load users
    }
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Kavopici.Web.Components.App>()
    .AddInteractiveServerRenderMode();

// Auto-open browser
var url = app.Urls.FirstOrDefault() ?? "http://localhost:5201";
Task.Run(async () =>
{
    await Task.Delay(500);
    try
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
    catch
    {
        // Ignore if browser can't be opened (e.g. headless environment)
    }
});

app.Run();
