using Kavopici.Models;

namespace Kavopici.Web.Services;

public class AppState
{
    public User? CurrentUser { get; set; }
    public bool IsLoggedIn => CurrentUser != null;
    public bool IsAdmin => CurrentUser?.IsAdmin == true;

    public event Action? OnChange;

    public void SetUser(User user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        OnChange?.Invoke();
    }
}
