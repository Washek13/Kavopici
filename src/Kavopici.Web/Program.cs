using System.Diagnostics;
using Kavopici.Data;
using Kavopici.Services;
using Kavopici.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database path: configurable, defaults to kavopici.db next to the executable
var dbPath = builder.Configuration["DatabasePath"] ?? "kavopici.db";
if (!Path.IsPathRooted(dbPath))
    dbPath = Path.Combine(AppContext.BaseDirectory, dbPath);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<KavopiciDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath};Pooling=False")
        .AddInterceptors(new SqliteWalInterceptor()));

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

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KavopiciDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
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
