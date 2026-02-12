using Kavopici.Services;

namespace Kavopici.Web.Services;

public class UpdateState
{
    public UpdateInfo? AvailableUpdate { get; private set; }
    public bool IsUpdateAvailable => AvailableUpdate != null;

    public event Action? OnChange;

    public void SetAvailableUpdate(UpdateInfo update)
    {
        AvailableUpdate = update;
        OnChange?.Invoke();
    }
}
