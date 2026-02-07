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
    }

    /// <summary>
    /// Must be called after construction to avoid circular DI deadlock.
    /// </summary>
    public void NavigateToInitialView()
    {
        _navigation.NavigateTo<LoginViewModel>();
    }

    private void OnCurrentViewModelChanged()
    {
        CurrentViewModel = _navigation.CurrentViewModel;
    }
}
