using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kavopici.Models;
using Kavopici.Services;

namespace Kavopici.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly INavigationService _navigation;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private User? selectedUser;

    [ObservableProperty]
    private ObservableCollection<User> users = new();

    [ObservableProperty]
    private bool isFirstRun;

    [ObservableProperty]
    private string newAdminName = string.Empty;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isLoading;

    public LoginViewModel(IUserService userService, INavigationService navigation, MainViewModel mainViewModel)
    {
        _userService = userService;
        _navigation = navigation;
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

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
            ErrorMessage = ex.Message;
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
}
