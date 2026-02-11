using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kavopici.Models;
using Kavopici.Services;

namespace Kavopici.ViewModels;

public partial class TastingNoteViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Name { get; init; } = "";

    [ObservableProperty]
    private bool isSelected;
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly ISessionService _sessionService;
    private readonly IRatingService _ratingService;
    private readonly IStatisticsService _statisticsService;
    private readonly INavigationService _navigation;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private TastingSession? todaySession;

    [ObservableProperty]
    private CoffeeBlend? todayBlend;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitRatingCommand))]
    private int selectedStars;

    [ObservableProperty]
    private string? comment;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitRatingCommand))]
    private bool hasRated;

    [ObservableProperty]
    private Rating? existingRating;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBlendSecret))]
    private bool isBlendRevealed;

    // Highlights
    [ObservableProperty]
    private string? topBlendName;

    [ObservableProperty]
    private double topBlendRating;

    [ObservableProperty]
    private string? mostActiveRater;

    [ObservableProperty]
    private int mostActiveRaterCount;

    [ObservableProperty]
    private bool hasHighlights;

    // Tasting notes
    [ObservableProperty]
    private ObservableCollection<TastingNoteViewModel> availableNotes = new();

    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            if (SetProperty(ref _currentUser, value))
            {
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(UserDisplayName));
            }
        }
    }

    public bool IsAdmin => CurrentUser?.IsAdmin == true;
    public string UserDisplayName => CurrentUser?.Name ?? string.Empty;
    public bool HasBlendToday => TodaySession != null;
    public bool ShowBlendSecret => HasBlendToday && !IsBlendRevealed;
    public bool CanShowRating => HasBlendToday && !HasRated;

    public DashboardViewModel(ISessionService sessionService, IRatingService ratingService,
        IStatisticsService statisticsService, INavigationService navigation, MainViewModel mainViewModel)
    {
        _sessionService = sessionService;
        _ratingService = ratingService;
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
            StatusMessage = null;

            TodaySession = await _sessionService.GetTodaySessionAsync();
            TodayBlend = TodaySession?.Blend;

            OnPropertyChanged(nameof(HasBlendToday));
            OnPropertyChanged(nameof(ShowBlendSecret));

            // Load tasting notes
            var notes = await _ratingService.GetAllTastingNotesAsync();
            AvailableNotes = new ObservableCollection<TastingNoteViewModel>(
                notes.Select(n => new TastingNoteViewModel { Id = n.Id, Name = n.Name }));

            // Check if user already rated this session
            if (TodaySession != null && CurrentUser != null)
            {
                ExistingRating = await _ratingService.GetUserRatingForSessionAsync(
                    CurrentUser.Id, TodaySession.Id);

                if (ExistingRating != null)
                {
                    HasRated = true;
                    IsBlendRevealed = true;
                    SelectedStars = ExistingRating.Stars;
                    Comment = ExistingRating.Comment;
                    StatusMessage = "Vaše hodnocení bylo uloženo.";

                    // Load selected notes
                    var selectedIds = await _ratingService.GetRatingNoteIdsAsync(ExistingRating.Id);
                    foreach (var note in AvailableNotes)
                        note.IsSelected = selectedIds.Contains(note.Id);
                }
                else
                {
                    HasRated = false;
                    IsBlendRevealed = false;
                    SelectedStars = 0;
                    Comment = null;
                }
            }

            OnPropertyChanged(nameof(CanShowRating));

            // Load highlights
            var stats = await _statisticsService.GetBlendStatisticsAsync();
            if (stats.Count > 0)
            {
                var top = stats.OrderByDescending(s => s.AverageRating).First();
                TopBlendName = top.BlendName;
                TopBlendRating = top.AverageRating;

                // Find most active rater
                var allRatings = stats.Sum(s => s.RatingCount);
                if (allRatings > 0 && CurrentUser != null)
                {
                    var userHistory = await _statisticsService.GetUserRatingHistoryAsync(CurrentUser.Id);
                    MostActiveRater = CurrentUser.Name;
                    MostActiveRaterCount = userHistory.Count;
                }

                HasHighlights = true;
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
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSubmitRating))]
    private async Task SubmitRatingAsync()
    {
        try
        {
            ErrorMessage = null;

            if (TodaySession == null || CurrentUser == null || TodayBlend == null) return;

            if (IsEditing && ExistingRating != null)
            {
                ExistingRating = await _ratingService.UpdateRatingAsync(
                    ExistingRating.Id, SelectedStars, Comment);
                IsEditing = false;
            }
            else
            {
                ExistingRating = await _ratingService.SubmitRatingAsync(
                    TodayBlend.Id, CurrentUser.Id, TodaySession.Id, SelectedStars, Comment);
            }

            // Save tasting notes
            var selectedNoteIds = AvailableNotes
                .Where(n => n.IsSelected)
                .Select(n => n.Id)
                .ToList();
            if (selectedNoteIds.Count > 0 && ExistingRating != null)
            {
                await _ratingService.SetRatingNotesAsync(ExistingRating.Id, selectedNoteIds);
            }

            HasRated = true;
            IsBlendRevealed = true;
            StatusMessage = "Vaše hodnocení bylo uloženo.";
            OnPropertyChanged(nameof(CanShowRating));
            SubmitRatingCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanSubmitRating() => SelectedStars >= 1 && SelectedStars <= 5
                                       && TodaySession != null
                                       && (!HasRated || IsEditing);

    [RelayCommand]
    private void EditRating()
    {
        IsEditing = true;
        HasRated = false;
        OnPropertyChanged(nameof(CanShowRating));
        SubmitRatingCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (ExistingRating != null)
        {
            SelectedStars = ExistingRating.Stars;
            Comment = ExistingRating.Comment;
        }
        IsEditing = false;
        HasRated = true;
        OnPropertyChanged(nameof(CanShowRating));
        SubmitRatingCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void NavigateToStatistics()
    {
        _navigation.NavigateTo<StatisticsViewModel>(vm => vm.CurrentUser = CurrentUser);
    }

    [RelayCommand]
    private void NavigateToAdmin()
    {
        _navigation.NavigateTo<AdminViewModel>();
    }

    [RelayCommand]
    private void Logout()
    {
        _mainViewModel.CurrentUser = null;
        _navigation.NavigateTo<LoginViewModel>();
    }
}
