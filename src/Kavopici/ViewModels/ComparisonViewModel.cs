using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kavopici.Models;
using Kavopici.Services;

namespace Kavopici.ViewModels;

public partial class ComparisonViewModel : ObservableObject
{
    private readonly IStatisticsService _statisticsService;
    private readonly INavigationService _navigation;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private ObservableCollection<BlendStatistics> allBlends = new();

    [ObservableProperty]
    private BlendStatistics? blendA;

    [ObservableProperty]
    private BlendStatistics? blendB;

    [ObservableProperty]
    private ObservableCollection<DistributionBar> distributionA = new();

    [ObservableProperty]
    private ObservableCollection<DistributionBar> distributionB = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public ComparisonViewModel(IStatisticsService statisticsService, INavigationService navigation,
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
            AllBlends = new ObservableCollection<BlendStatistics>(stats);
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

    partial void OnBlendAChanged(BlendStatistics? value)
    {
        if (value != null)
            DistributionA = BuildDistribution(value);
    }

    partial void OnBlendBChanged(BlendStatistics? value)
    {
        if (value != null)
            DistributionB = BuildDistribution(value);
    }

    private static ObservableCollection<DistributionBar> BuildDistribution(BlendStatistics stats)
    {
        var maxCount = stats.Distribution.Max();
        var bars = new List<DistributionBar>();
        for (int i = 4; i >= 0; i--)
        {
            bars.Add(new DistributionBar
            {
                Label = $"{i + 1} ★",
                Count = stats.Distribution[i],
                BarWidth = maxCount > 0 ? (stats.Distribution[i] / (double)maxCount) * 150 : 0
            });
        }
        return new ObservableCollection<DistributionBar>(bars);
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigation.NavigateTo<StatisticsViewModel>(vm =>
            vm.CurrentUser = _mainViewModel.CurrentUser);
    }
}
