using CommunityToolkit.Mvvm.ComponentModel;

namespace Kavopici.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ObservableObject? CurrentViewModel { get; private set; }

    public event Action? CurrentViewModelChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        var viewModel = (TViewModel)_serviceProvider.GetService(typeof(TViewModel))!;
        CurrentViewModel = viewModel;
        CurrentViewModelChanged?.Invoke();
    }

    public void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ObservableObject
    {
        var viewModel = (TViewModel)_serviceProvider.GetService(typeof(TViewModel))!;
        configure(viewModel);
        CurrentViewModel = viewModel;
        CurrentViewModelChanged?.Invoke();
    }
}
