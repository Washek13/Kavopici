using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kavopici.Models;
using Kavopici.Services;

namespace Kavopici.ViewModels;

public class DistributionBar
{
    public string Label { get; init; } = "";
    public int Count { get; init; }
    public double BarWidth { get; init; }
}

public partial class BlendDetailViewModel : ObservableObject
{
    private readonly IStatisticsService _statisticsService;
    private readonly IRatingService _ratingService;
    private readonly INavigationService _navigation;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private BlendStatistics? blendStats;

    [ObservableProperty]
    private ObservableCollection<Rating> ratings = new();

    [ObservableProperty]
    private ObservableCollection<DistributionBar> distributionBars = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    private int _blendId;
    public int BlendId
    {
        get => _blendId;
        set => SetProperty(ref _blendId, value);
    }

    public BlendDetailViewModel(IStatisticsService statisticsService, IRatingService ratingService,
        INavigationService navigation, MainViewModel mainViewModel)
    {
        _statisticsService = statisticsService;
        _ratingService = ratingService;
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

            var allStats = await _statisticsService.GetBlendStatisticsAsync();
            BlendStats = allStats.FirstOrDefault(s => s.BlendId == BlendId);

            if (BlendStats != null)
            {
                var maxCount = BlendStats.Distribution.Max();
                var bars = new List<DistributionBar>();
                for (int i = 4; i >= 0; i--)
                {
                    bars.Add(new DistributionBar
                    {
                        Label = $"{i + 1} ★",
                        Count = BlendStats.Distribution[i],
                        BarWidth = maxCount > 0 ? (BlendStats.Distribution[i] / (double)maxCount) * 300 : 0
                    });
                }
                DistributionBars = new ObservableCollection<DistributionBar>(bars);
            }

            var blendRatings = await _ratingService.GetRatingsForBlendAsync(BlendId);
            Ratings = new ObservableCollection<Rating>(blendRatings);
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
    private void GoBack()
    {
        _navigation.NavigateTo<StatisticsViewModel>(vm =>
            vm.CurrentUser = _mainViewModel.CurrentUser);
    }
}
