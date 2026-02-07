using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kavopici.Models;
using Kavopici.Models.Enums;
using Kavopici.Services;
using Microsoft.Win32;

namespace Kavopici.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly IBlendService _blendService;
    private readonly ISessionService _sessionService;
    private readonly ICsvExportService _csvExportService;
    private readonly IPrintService _printService;
    private readonly INavigationService _navigation;
    private readonly MainViewModel _mainViewModel;

    // --- Users Tab ---
    [ObservableProperty]
    private ObservableCollection<User> allUsers = new();

    [ObservableProperty]
    private string newUserName = string.Empty;

    // --- Blends Tab ---
    [ObservableProperty]
    private ObservableCollection<CoffeeBlend> allBlends = new();

    [ObservableProperty]
    private string newBlendName = string.Empty;

    [ObservableProperty]
    private string newBlendRoaster = string.Empty;

    [ObservableProperty]
    private string? newBlendOrigin;

    [ObservableProperty]
    private RoastLevel newBlendRoastLevel;

    [ObservableProperty]
    private User? newBlendSupplier;

    // --- Blend of the Day Tab ---
    [ObservableProperty]
    private ObservableCollection<CoffeeBlend> availableBlends = new();

    [ObservableProperty]
    private CoffeeBlend? selectedBlendOfDay;

    [ObservableProperty]
    private TastingSession? currentSession;

    [ObservableProperty]
    private ObservableCollection<TastingSession> sessionHistory = new();

    // --- Common ---
    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private int selectedTabIndex;

    public RoastLevel[] RoastLevels => Enum.GetValues<RoastLevel>();

    public AdminViewModel(IUserService userService, IBlendService blendService,
        ISessionService sessionService, ICsvExportService csvExportService,
        IPrintService printService, INavigationService navigation, MainViewModel mainViewModel)
    {
        _userService = userService;
        _blendService = blendService;
        _sessionService = sessionService;
        _csvExportService = csvExportService;
        _printService = printService;
        _navigation = navigation;
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private async Task LoadAllAsync()
    {
        await Task.WhenAll(
            LoadUsersAsync(),
            LoadBlendsAsync(),
            LoadCurrentSessionAsync()
        );
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            ErrorMessage = null;
            var users = await _userService.GetAllUsersAsync();
            AllUsers = new ObservableCollection<User>(users.Where(u => u.IsActive));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        try
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(NewUserName))
            {
                ErrorMessage = "Zadejte jméno uživatele.";
                return;
            }

            await _userService.CreateUserAsync(NewUserName.Trim());
            NewUserName = string.Empty;
            StatusMessage = "Uživatel byl přidán.";
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateUserAsync(User user)
    {
        try
        {
            ErrorMessage = null;
            await _userService.DeactivateUserAsync(user.Id);
            StatusMessage = $"Uživatel {user.Name} byl deaktivován.";
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ToggleAdminAsync(User user)
    {
        try
        {
            ErrorMessage = null;
            await _userService.ToggleAdminAsync(user.Id);
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadBlendsAsync()
    {
        try
        {
            ErrorMessage = null;
            var blends = await _blendService.GetActiveBlendsAsync();
            AllBlends = new ObservableCollection<CoffeeBlend>(blends);
            AvailableBlends = new ObservableCollection<CoffeeBlend>(blends);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddBlendAsync()
    {
        try
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(NewBlendName))
            {
                ErrorMessage = "Zadejte název směsi.";
                return;
            }
            if (string.IsNullOrWhiteSpace(NewBlendRoaster))
            {
                ErrorMessage = "Zadejte název pražírny.";
                return;
            }
            if (NewBlendSupplier == null)
            {
                ErrorMessage = "Vyberte dodavatele.";
                return;
            }

            await _blendService.CreateBlendAsync(
                NewBlendName.Trim(),
                NewBlendRoaster.Trim(),
                NewBlendOrigin?.Trim(),
                NewBlendRoastLevel,
                NewBlendSupplier.Id
            );

            NewBlendName = string.Empty;
            NewBlendRoaster = string.Empty;
            NewBlendOrigin = null;
            NewBlendRoastLevel = RoastLevel.Medium;
            NewBlendSupplier = null;

            StatusMessage = "Směs byla přidána.";
            await LoadBlendsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateBlendAsync(CoffeeBlend blend)
    {
        try
        {
            ErrorMessage = null;
            await _blendService.DeactivateBlendAsync(blend.Id);
            StatusMessage = $"Směs {blend.Name} byla odstraněna.";
            await LoadBlendsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadCurrentSessionAsync()
    {
        try
        {
            ErrorMessage = null;
            CurrentSession = await _sessionService.GetTodaySessionAsync();
            var history = await _sessionService.GetSessionHistoryAsync();
            SessionHistory = new ObservableCollection<TastingSession>(history);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SetBlendOfDayAsync()
    {
        try
        {
            ErrorMessage = null;
            if (SelectedBlendOfDay == null)
            {
                ErrorMessage = "Vyberte směs.";
                return;
            }

            CurrentSession = await _sessionService.SetBlendOfTheDayAsync(SelectedBlendOfDay.Id);
            StatusMessage = $"Dnešní káva nastavena: {SelectedBlendOfDay.Name}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        try
        {
            ErrorMessage = null;
            var dialog = new SaveFileDialog
            {
                Filter = "CSV soubory (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = $"kavopici_export_{DateTime.Now:yyyy-MM-dd}"
            };

            if (dialog.ShowDialog() == true)
            {
                await _csvExportService.ExportStatisticsAsync(dialog.FileName);
                StatusMessage = "Data byla exportována.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task PrintReportAsync()
    {
        try
        {
            ErrorMessage = null;
            await _printService.PrintStatisticsReportAsync();
            StatusMessage = "Tisk byl odeslán.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigation.NavigateTo<DashboardViewModel>(vm =>
            vm.CurrentUser = _mainViewModel.CurrentUser);
    }
}
