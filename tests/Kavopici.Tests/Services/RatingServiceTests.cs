using Kavopici.Models;
using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Xunit;

namespace Kavopici.Tests.Services;

public class RatingServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly RatingService _ratingService;
    private readonly UserService _userService;
    private readonly BlendService _blendService;
    private readonly SessionService _sessionService;

    public RatingServiceTests()
    {
        _factory = new TestDbContextFactory();
        _ratingService = new RatingService(_factory);
        _userService = new UserService(_factory);
        _blendService = new BlendService(_factory);
        _sessionService = new SessionService(_factory);
    }

    private async Task<(User user, TastingSession session)> SetupAsync()
    {
        var user = await _userService.CreateUserAsync("Test User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test Blend", "Test Roaster", "Origin",
            RoastLevel.Medium, user.Id);
        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);
        return (user, session);
    }

    [Fact]
    public async Task SubmitRatingAsync_CreatesRating()
    {
        var (user, session) = await SetupAsync();

        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, "Great coffee!");

        Assert.Equal(4, rating.Stars);
        Assert.Equal("Great coffee!", rating.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_DuplicateRating_Throws()
    {
        var (user, session) = await SetupAsync();

        await _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 4, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 5, null));
    }

    [Fact]
    public async Task SubmitRatingAsync_InvalidStars_Throws()
    {
        var (user, session) = await SetupAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 0, null));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 6, null));
    }

    [Fact]
    public async Task GetUserRatingForSessionAsync_NoRating_ReturnsNull()
    {
        var (user, session) = await SetupAsync();

        var result = await _ratingService.GetUserRatingForSessionAsync(user.Id, session.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserRatingForSessionAsync_WithRating_ReturnsRating()
    {
        var (user, session) = await SetupAsync();
        await _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 3, "OK");

        var result = await _ratingService.GetUserRatingForSessionAsync(user.Id, session.Id);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Stars);
    }

    [Fact]
    public async Task UpdateRatingAsync_UpdatesStarsAndComment()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 3, "OK");

        var updated = await _ratingService.UpdateRatingAsync(rating.Id, 5, "Actually great!");

        Assert.Equal(5, updated.Stars);
        Assert.Equal("Actually great!", updated.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_NullComment_StoresNull()
    {
        var (user, session) = await SetupAsync();

        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, null);

        Assert.Null(rating.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_WhitespaceComment_StoresNull()
    {
        var (user, session) = await SetupAsync();

        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, "   ");

        Assert.Null(rating.Comment);
    }

    [Fact]
    public async Task UpdateRatingAsync_InvalidStars_Throws()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 3, null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _ratingService.UpdateRatingAsync(rating.Id, 0, null));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _ratingService.UpdateRatingAsync(rating.Id, 6, null));
    }

    [Fact]
    public async Task UpdateRatingAsync_NonExistentRating_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ratingService.UpdateRatingAsync(9999, 3, null));
    }

    [Fact]
    public async Task UpdateRatingAsync_WhitespaceComment_StoresNull()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 3, "Initial");

        var updated = await _ratingService.UpdateRatingAsync(rating.Id, 3, "   ");

        Assert.Null(updated.Comment);
    }

    [Fact]
    public async Task UpdateRatingAsync_CommentTrimmed()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 3, null);

        var updated = await _ratingService.UpdateRatingAsync(rating.Id, 4, "  Trimmed  ");

        Assert.Equal("Trimmed", updated.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_CommentTrimmed()
    {
        var (user, session) = await SetupAsync();

        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, "  Spaces around  ");

        Assert.Equal("Spaces around", rating.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_BoundaryStars_Valid()
    {
        var (user, session) = await SetupAsync();

        var rating1 = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 1, null);
        Assert.Equal(1, rating1.Stars);

        // Create a second user to rate the same session with 5 stars
        var user2 = await _userService.CreateUserAsync("User2");
        var rating5 = await _ratingService.SubmitRatingAsync(
            session.BlendId, user2.Id, session.Id, 5, null);
        Assert.Equal(5, rating5.Stars);
    }

    [Fact]
    public async Task GetRatingsForSessionAsync_ReturnsRatingsWithUser()
    {
        var (user1, session) = await SetupAsync();
        var user2 = await _userService.CreateUserAsync("User2");

        await _ratingService.SubmitRatingAsync(session.BlendId, user1.Id, session.Id, 4, null);
        await _ratingService.SubmitRatingAsync(session.BlendId, user2.Id, session.Id, 3, null);

        var ratings = await _ratingService.GetRatingsForSessionAsync(session.Id);

        Assert.Equal(2, ratings.Count);
        Assert.All(ratings, r => Assert.NotNull(r.User));
    }

    [Fact]
    public async Task GetRatingsForSessionAsync_NoRatings_ReturnsEmpty()
    {
        var (_, session) = await SetupAsync();

        var ratings = await _ratingService.GetRatingsForSessionAsync(session.Id);

        Assert.Empty(ratings);
    }

    [Fact]
    public async Task GetRatingsForBlendAsync_ReturnsRatingsWithRelations()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, null);
        await _ratingService.SetRatingNotesAsync(rating.Id, new List<int> { 1, 2 });

        var ratings = await _ratingService.GetRatingsForBlendAsync(session.BlendId);

        Assert.Single(ratings);
        Assert.NotNull(ratings[0].User);
        Assert.NotNull(ratings[0].Session);
        Assert.NotNull(ratings[0].RatingTastingNotes);
        Assert.Equal(2, ratings[0].RatingTastingNotes.Count);
    }

    [Fact]
    public async Task GetAllTastingNotesAsync_ReturnsSeededNotes()
    {
        var notes = await _ratingService.GetAllTastingNotesAsync();

        Assert.Equal(8, notes.Count);
        var names = notes.Select(n => n.Name).ToList();
        Assert.Contains("Ovocná", names);
        Assert.Contains("Ořechová", names);
        Assert.Contains("Čokoládová", names);
        Assert.Contains("Karamelová", names);
        Assert.Contains("Květinová", names);
        Assert.Contains("Kořeněná", names);
        Assert.Contains("Citrusová", names);
        Assert.Contains("Medová", names);
    }

    [Fact]
    public async Task SetRatingNotesAsync_AddsNotes()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, null);

        await _ratingService.SetRatingNotesAsync(rating.Id, new List<int> { 1, 3, 5 });

        var noteIds = await _ratingService.GetRatingNoteIdsAsync(rating.Id);
        Assert.Equal(3, noteIds.Count);
        Assert.Contains(1, noteIds);
        Assert.Contains(3, noteIds);
        Assert.Contains(5, noteIds);
    }

    [Fact]
    public async Task SetRatingNotesAsync_ReplacesExistingNotes()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, null);

        await _ratingService.SetRatingNotesAsync(rating.Id, new List<int> { 1, 2 });
        await _ratingService.SetRatingNotesAsync(rating.Id, new List<int> { 3, 4 });

        var noteIds = await _ratingService.GetRatingNoteIdsAsync(rating.Id);
        Assert.Equal(2, noteIds.Count);
        Assert.Contains(3, noteIds);
        Assert.Contains(4, noteIds);
        Assert.DoesNotContain(1, noteIds);
        Assert.DoesNotContain(2, noteIds);
    }

    [Fact]
    public async Task SetRatingNotesAsync_EmptyList_ClearsNotes()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, null);

        await _ratingService.SetRatingNotesAsync(rating.Id, new List<int> { 1, 2 });
        await _ratingService.SetRatingNotesAsync(rating.Id, new List<int>());

        var noteIds = await _ratingService.GetRatingNoteIdsAsync(rating.Id);
        Assert.Empty(noteIds);
    }

    [Fact]
    public async Task GetRatingNoteIdsAsync_NoNotes_ReturnsEmpty()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, null);

        var noteIds = await _ratingService.GetRatingNoteIdsAsync(rating.Id);

        Assert.Empty(noteIds);
    }

    public void Dispose() => _factory.Dispose();
}
