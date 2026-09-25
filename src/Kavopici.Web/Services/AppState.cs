using Kavopici.Models;
using Kavopici.Models.Enums;

namespace Kavopici.Web.Services;

public class AppState
{
    public User? CurrentUser { get; set; }
    public bool IsLoggedIn => CurrentUser != null;
    public bool IsAdmin => CurrentUser?.IsAdmin == true;

    public Theme CurrentTheme { get; private set; } = Theme.Coffee;

    /// <summary>Emoji representing a single blend in the active theme.</summary>
    public string BlendEmoji => CurrentTheme == Theme.Tea ? "🍵" : "☕";

    public event Action? OnChange;
    public event Action? OnThemeChange;

    public void SetUser(User user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        // Reset theme so the next database (after switching) gets a clean default
        // until its theme is loaded.
        CurrentTheme = Theme.Coffee;
        OnChange?.Invoke();
        OnThemeChange?.Invoke();
    }

    public void SetTheme(Theme theme)
    {
        if (CurrentTheme == theme) return;
        CurrentTheme = theme;
        OnThemeChange?.Invoke();
    }
}
