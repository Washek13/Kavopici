using System.Diagnostics;
using Kavopici.Data;
using Kavopici.Services;
using Kavopici.Web.Services;
using Microsoft.EntityFrameworkCore;

// Single-instance detection: if an instance is already running, open browser and exit
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .Build();
var serverUrl = config["Urls"] ?? "http://localhost:5201";

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
try
{
    await httpClient.GetAsync(serverUrl);
    // Server responded — another instance is already running
    Process.Start(new ProcessStartInfo { FileName = serverUrl, UseShellExecute = true });
    return;
}
catch
{
    // No response — no instance running, continue with normal startup
}

var builder = WebApplication.CreateBuilder(args);

// Settings (DB path stored in %APPDATA%/Kavopici/settings.json)
var settingsService = new AppSettingsService();
settingsService.Load();
builder.Services.AddSingleton<IAppSettingsService>(settingsService);
builder.Services.AddSingleton<IDbContextFactory<KavopiciDbContext>, KavopiciDbContextFactory>();

builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

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
builder.Services.AddSingleton<IUpdateService, UpdateService>();
builder.Services.AddSingleton<UpdateState>();

var app = builder.Build();

// Apply migrations on startup (only if a database is already configured)
if (!string.IsNullOrEmpty(settingsService.DatabasePath) && File.Exists(settingsService.DatabasePath))
{
    var migrated = false;
    for (var attempt = 1; attempt <= 6 && !migrated; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KavopiciDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                await db.Database.MigrateAsync();
            }
            migrated = true;
        }
        catch (Exception ex) when (attempt < 6)
        {
            Console.Error.WriteLine(
                $"Database migration attempt {attempt}/6 failed: {ex.Message}. Retrying in 5s...");
            await Task.Delay(5000);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Database migration failed: {ex.Message}");
        }
    }
}

app.UseStaticFiles();

var supportedCultures = new[] { "cs", "sk", "en", "de" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("cs")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseAntiforgery();

app.MapRazorComponents<Kavopici.Web.Components.App>()
    .AddInteractiveServerRenderMode();

// Check for updates in the background
_ = Task.Run(async () =>
{
    var updateService = app.Services.GetRequiredService<IUpdateService>();
    var update = await updateService.CheckForUpdateAsync();
    if (update is not null)
    {
        var updateState = app.Services.GetRequiredService<UpdateState>();
        updateState.SetAvailableUpdate(update);
    }
});

// Auto-open browser with splash screen
var url = app.Urls.FirstOrDefault() ?? "http://localhost:5201";
var splashPath = Path.Combine(Path.GetTempPath(), "kavopici-splash.html");
try
{
    var iconPath = Path.Combine(app.Environment.WebRootPath, "icon-80.png");
    var iconBase64 = Convert.ToBase64String(File.ReadAllBytes(iconPath));
    var splashHtml = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>Kávopíči</title>
<style>
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  body {{
    background: #FFF8F0;
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    font-family: 'Segoe UI', system-ui, sans-serif;
  }}
  .splash {{
    text-align: center;
  }}
  .splash img {{
    width: 80px;
    height: 80px;
    margin-bottom: 16px;
  }}
  .splash h1 {{
    font-family: 'Georgia', serif;
    font-size: 42px;
    color: #2C1810;
    letter-spacing: 3px;
    margin-bottom: 24px;
  }}
  .loading {{
    color: #4A2C2A;
    font-size: 16px;
  }}
  .loading::after {{
    content: '';
    animation: dots 1.5s steps(4, end) infinite;
  }}
  @keyframes dots {{
    0% {{ content: ''; }}
    25% {{ content: '.'; }}
    50% {{ content: '..'; }}
    75% {{ content: '...'; }}
  }}
</style>
</head>
<body>
<div class=""splash"">
  <img src=""data:image/png;base64,{iconBase64}"" alt=""Kávopíči"">
  <h1>KÁVOPÍČI</h1>
  <div class=""loading"">Načítání</div>
</div>
<script>
  var target = '{url}';
  var timer = setInterval(function() {{
    fetch(target, {{ mode: 'no-cors' }}).then(function() {{
      clearInterval(timer);
      window.location.href = target;
    }}).catch(function() {{}});
  }}, 500);
</script>
</body>
</html>";
    File.WriteAllText(splashPath, splashHtml);
    Process.Start(new ProcessStartInfo { FileName = splashPath, UseShellExecute = true });
}
catch
{
    // Ignore if browser can't be opened (e.g. headless environment)
}

app.Lifetime.ApplicationStopping.Register(() =>
{
    try { File.Delete(splashPath); } catch { }
});

app.Run();
