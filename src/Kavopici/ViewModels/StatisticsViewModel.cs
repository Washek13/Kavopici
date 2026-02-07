using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kavopici.Models;
using Kavopici.Services;

namespace Kavopici.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly IStatisticsService _statisticsService;
    private readonly INavigationService _navigation;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private ObservableCollection<BlendStatistics> blendStats = new();

    [ObservableProperty]
    private ObservableCollection<Rating> userRatings = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private int selectedTabIndex;

    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    public StatisticsViewModel(IStatisticsService statisticsService, INavigationService navigation,
        MainViewModel mainViewModel)
    {
        _statisticsService = statisticsService;
        _navigation = navigation;
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var stats = await _statisticsService.GetBlendStatisticsAsync();
            BlendStats = new ObservableCollection<BlendStatistics>(stats);

            if (CurrentUser != null)
            {
                var history = await _statisticsService.GetUserRatingHistoryAsync(CurrentUser.Id);
                UserRatings = new ObservableCollection<Rating>(history);
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

    [RelayCommand]
    private void SortBy(string columnName)
    {
        var sorted = columnName switch
        {
            "BlendName" => BlendStats.OrderBy(s => s.BlendName).ToList(),
            "AverageRating" => BlendStats.OrderByDescending(s => s.AverageRating).ToList(),
            "RatingCount" => BlendStats.OrderByDescending(s => s.RatingCount).ToList(),
            "Roaster" => BlendStats.OrderBy(s => s.Roaster).ToList(),
            "SupplierName" => BlendStats.OrderBy(s => s.SupplierName).ToList(),
            _ => BlendStats.ToList()
        };
        BlendStats = new ObservableCollection<BlendStatistics>(sorted);
    }

    [RelayCommand]
    private void NavigateToDetail(BlendStatistics blend)
    {
        _navigation.NavigateTo<BlendDetailViewModel>(vm =>
            vm.BlendId = blend.BlendId);
    }

    [RelayCommand]
    private void NavigateToComparison()
    {
        _navigation.NavigateTo<ComparisonViewModel>();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigation.NavigateTo<DashboardViewModel>(vm =>
            vm.CurrentUser = _mainViewModel.CurrentUser);
    }
}
