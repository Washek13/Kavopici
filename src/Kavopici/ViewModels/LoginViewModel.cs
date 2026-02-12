using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kavopici.Data;
using Kavopici.Models;
using Kavopici.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace Kavopici.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly INavigationService _navigation;
    private readonly MainViewModel _mainViewModel;
    private readonly IAppSettingsService _appSettings;
    private readonly IDbContextFactory<KavopiciDbContext> _dbContextFactory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private User? selectedUser;

    [ObservableProperty]
    private ObservableCollection<User> users = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFirstRun))]
    [NotifyPropertyChangedFor(nameof(ShowNormalLogin))]
    private bool isFirstRun;

    [ObservableProperty]
    private string newAdminName = string.Empty;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFirstRun))]
    [NotifyPropertyChangedFor(nameof(ShowNormalLogin))]
    private bool isDatabaseNotSelected;

    [ObservableProperty]
    private string? selectedDatabasePath;

    public bool ShowFirstRun => !IsDatabaseNotSelected && IsFirstRun;
    public bool ShowNormalLogin => !IsDatabaseNotSelected && !IsFirstRun;

    public LoginViewModel(
        IUserService userService,
        INavigationService navigation,
        MainViewModel mainViewModel,
        IAppSettingsService appSettings,
        IDbContextFactory<KavopiciDbContext> dbContextFactory)
    {
        _userService = userService;
        _navigation = navigation;
        _mainViewModel = mainViewModel;
        _appSettings = appSettings;
        _dbContextFactory = dbContextFactory;
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var dbPath = _appSettings.DatabasePath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                if (!string.IsNullOrEmpty(dbPath) && !File.Exists(dbPath))
                    _appSettings.SetDatabasePath(null);

                IsDatabaseNotSelected = true;
                IsFirstRun = false;
                SelectedDatabasePath = null;
                return;
            }

            IsDatabaseNotSelected = false;
            SelectedDatabasePath = dbPath;

            await Task.Run(() =>
            {
                using var db = _dbContextFactory.CreateDbContext();
                db.Database.Migrate();
            });

            var hasUsers = await _userService.HasAnyUsersAsync();
            IsFirstRun = !hasUsers;

            if (hasUsers)
            {
                var activeUsers = await _userService.GetActiveUsersAsync();
                Users = new ObservableCollection<User>(activeUsers);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Chyba při otevírání databáze: {ex.Message}";
            IsDatabaseNotSelected = true;
            IsFirstRun = false;
            SelectedDatabasePath = null;
            _appSettings.SetDatabasePath(null);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private void Login()
    {
        if (SelectedUser == null) return;

        _mainViewModel.CurrentUser = SelectedUser;
        _navigation.NavigateTo<DashboardViewModel>(vm => vm.CurrentUser = SelectedUser);
    }

    private bool CanLogin() => SelectedUser is not null;

    [RelayCommand]
    private async Task CreateFirstAdminAsync()
    {
        try
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(NewAdminName))
            {
                ErrorMessage = "Zadejte jméno administrátora.";
                return;
            }

            var admin = await _userService.CreateUserAsync(NewAdminName.Trim(), isAdmin: true);
            IsFirstRun = false;
            NewAdminName = string.Empty;

            // Reload users and auto-select the new admin
            await LoadUsersAsync();
            SelectedUser = Users.FirstOrDefault(u => u.Id == admin.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CreateNewDatabaseAsync()
    {
        try
        {
            ErrorMessage = null;

            var dialog = new SaveFileDialog
            {
                Title = "Vytvořit novou databázi",
                Filter = "SQLite databáze (*.db)|*.db",
                DefaultExt = ".db",
                FileName = "kavopici.db"
            };

            if (dialog.ShowDialog() != true)
                return;

            var path = dialog.FileName;

            if (File.Exists(path))
                File.Delete(path);

            _appSettings.SetDatabasePath(path);
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Chyba při vytváření databáze: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenExistingDatabaseAsync()
    {
        try
        {
            ErrorMessage = null;

            var dialog = new OpenFileDialog
            {
                Title = "Vybrat existující databázi",
                Filter = "SQLite databáze (*.db)|*.db|Všechny soubory (*.*)|*.*",
                DefaultExt = ".db"
            };

            if (dialog.ShowDialog() != true)
                return;

            _appSettings.SetDatabasePath(dialog.FileName);
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Chyba při otevírání databáze: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseDatabase()
    {
        _appSettings.SetDatabasePath(null);
        Users.Clear();
        SelectedUser = null;
        SelectedDatabasePath = null;
        IsDatabaseNotSelected = true;
        IsFirstRun = false;
        ErrorMessage = null;
    }
}
