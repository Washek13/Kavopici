using CommunityToolkit.Mvvm.ComponentModel;
using Kavopici.Models;
using Kavopici.Services;

namespace Kavopici.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ObservableObject? currentViewModel;

    [ObservableProperty]
    private User? currentUser;

    public MainViewModel(INavigationService navigation)
    {
        _navigation = navigation;
        _navigation.CurrentViewModelChanged += OnCurrentViewModelChanged;

        // Start at login
        _navigation.NavigateTo<LoginViewModel>();
    }

    private void OnCurrentViewModelChanged()
    {
        CurrentViewModel = _navigation.CurrentViewModel;
    }
}
