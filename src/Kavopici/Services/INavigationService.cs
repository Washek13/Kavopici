using CommunityToolkit.Mvvm.ComponentModel;

namespace Kavopici.Services;

public interface INavigationService
{
    ObservableObject? CurrentViewModel { get; }
    event Action? CurrentViewModelChanged;
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ObservableObject;
}
