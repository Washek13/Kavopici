using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Kavopici.Data;
using Kavopici.Services;
using Kavopici.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kavopici;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Settings (DB path stored in %APPDATA%/Kavopici)
                var settingsService = new AppSettingsService();
                settingsService.Load();
                if (settingsService.DatabasePath is null)
                {
                    var defaultPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Kavopici", "kavopici.db");
                    Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);
                    settingsService.SetDatabasePath(defaultPath);
                }
                services.AddSingleton<IAppSettingsService>(settingsService);
                services.AddSingleton<IDbContextFactory<KavopiciDbContext>, KavopiciDbContextFactory>();

                // Services
                services.AddTransient<IUserService, UserService>();
                services.AddTransient<IBlendService, BlendService>();
                services.AddTransient<ISessionService, SessionService>();
                services.AddTransient<IRatingService, RatingService>();
                services.AddTransient<IStatisticsService, StatisticsService>();
                services.AddTransient<ICsvExportService, CsvExportService>();
                services.AddTransient<IPrintService, PrintService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IUpdateService, UpdateService>();

                // ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<StatisticsViewModel>();
                services.AddTransient<AdminViewModel>();
                services.AddTransient<BlendDetailViewModel>();
                services.AddTransient<ComparisonViewModel>();
                services.AddSingleton<MainViewModel>();

                // Windows
                services.AddSingleton<MainWindow>();
            })
            .Build();

        host.Start();

        // Apply migrations on startup
        try
        {
            using var scope = host.Services.CreateScope();
            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<KavopiciDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nelze se připojit k databázi. Zkontrolujte síťové připojení.\n\nChyba: {ex.Message}",
                "Kávopíči — Chyba",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.MainWindow = host.Services.GetRequiredService<MainWindow>();

        var mainViewModel = host.Services.GetRequiredService<MainViewModel>();
        mainViewModel.NavigateToInitialView();

        app.MainWindow.Show();

        // Check for updates in the background
        _ = Task.Run(async () =>
        {
            var updateService = host.Services.GetRequiredService<IUpdateService>();
            var update = await updateService.CheckForUpdateAsync();
            if (update is not null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        $"Je dostupná nová verze {update.Version}.\n\n{update.ReleaseNotes}\n\nChcete aktualizovat?",
                        "Kávopíči — Aktualizace",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = updateService.DownloadAndInstallUpdateAsync(update);
                    }
                });
            }
        });

        app.Run();
    }
}
