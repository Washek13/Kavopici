using Kavopici.Models.Enums;
using Microsoft.Extensions.Localization;

namespace Kavopici.Web.Services;

public class ThemedLocalizer : IThemedLocalizer
{
    private readonly IStringLocalizer<SharedResources> _inner;
    private readonly AppState _state;

    public ThemedLocalizer(IStringLocalizer<SharedResources> inner, AppState state)
    {
        _inner = inner;
        _state = state;
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (_state.CurrentTheme == Theme.Tea)
            {
                var themed = _inner[name + "_Tea"];
                if (!themed.ResourceNotFound) return themed;
            }
            return _inner[name];
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (_state.CurrentTheme == Theme.Tea)
            {
                var themed = _inner[name + "_Tea", arguments];
                if (!themed.ResourceNotFound) return themed;
            }
            return _inner[name, arguments];
        }
    }
}
