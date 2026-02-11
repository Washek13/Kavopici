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
                // Settings & Database
                services.AddSingleton<IAppSettingsService, AppSettingsService>();
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

        var settingsService = host.Services.GetRequiredService<IAppSettingsService>();
        settingsService.Load();

        var app = new App();
        app.InitializeComponent();
        app.MainWindow = host.Services.GetRequiredService<MainWindow>();

        var mainViewModel = host.Services.GetRequiredService<MainViewModel>();
        mainViewModel.NavigateToInitialView();

        app.MainWindow.Show();
        app.Run();
    }
}
