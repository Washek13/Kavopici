using Microsoft.Extensions.Localization;

namespace Kavopici.Web.Services;

/// <summary>
/// Theme-aware wrapper around <see cref="IStringLocalizer{SharedResources}"/>. When the active
/// theme is Tea and a "{key}_Tea" variant exists in the resource file, that variant is
/// returned; otherwise the base key is used. This lets the codebase keep using a single
/// resource file while overriding only the wording that differs between themes.
/// </summary>
public interface IThemedLocalizer
{
    LocalizedString this[string name] { get; }
    LocalizedString this[string name, params object[] arguments] { get; }
}
