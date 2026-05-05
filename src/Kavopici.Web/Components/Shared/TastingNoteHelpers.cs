using System.Collections.Generic;

namespace Kavopici.Web.Components.Shared;

public static class TastingNoteHelpers
{
    /// <summary>
    /// Maps a tasting-note resource key (e.g. "TastingNote_1") OR a localized
    /// display name to a hex color. Falls back to a coffee-brown when no match.
    /// </summary>
    public static string NoteColor(string nameOrKey)
    {
        if (string.IsNullOrWhiteSpace(nameOrKey)) return "#6F4E37";

        // Try by ID-suffix (TastingNote_1..8) first, since that's stable across locales.
        if (nameOrKey.StartsWith("TastingNote_", System.StringComparison.OrdinalIgnoreCase))
        {
            var idPart = nameOrKey[12..];
            if (int.TryParse(idPart, out var id) && _byId.TryGetValue(id, out var cId))
                return cId;
        }

        // Try by Czech display name (the design's source of truth).
        if (_byCzech.TryGetValue(nameOrKey, out var c)) return c;

        return "#6F4E37";
    }

    public static string NoteColorById(int id)
        => _byId.TryGetValue(id, out var c) ? c : "#6F4E37";

    // Tasting note table — IDs match the order the app seeds them in.
    // Coffee theme: Fruity, Nutty, Chocolatey, Caramel, Floral, Spiced, Citrusy, Honey
    // Tea theme: Floral, Fruity, Grassy, Nutty, Earthy, Woody, Spiced, Sweet
    private static readonly Dictionary<int, string> _byId = new()
    {
        { 1, "#C2185B" }, // Fruity / Floral (tea)
        { 2, "#8D6E63" }, // Nutty / Fruity (tea)
        { 3, "#5D4037" }, // Chocolatey / Grassy
        { 4, "#D4A017" }, // Caramel / Nutty
        { 5, "#9C27B0" }, // Floral / Earthy
        { 6, "#BF360C" }, // Spiced / Woody
        { 7, "#F9A825" }, // Citrusy / Spiced
        { 8, "#FF8F00" }, // Honey / Sweet
    };

    private static readonly Dictionary<string, string> _byCzech = new()
    {
        { "Ovocné", "#C2185B" },
        { "Oříškové", "#8D6E63" },
        { "Čokoláda", "#5D4037" },
        { "Karamel", "#D4A017" },
        { "Květinové", "#9C27B0" },
        { "Kořeněné", "#BF360C" },
        { "Citrusové", "#F9A825" },
        { "Med", "#FF8F00" },
        // Tea
        { "Trávové", "#7CB342" },
        { "Zemité", "#6D4C41" },
        { "Dřevité", "#5D4037" },
        { "Sladké", "#FF8F00" }
    };

    public static IReadOnlyList<string> CoffeeNoteNames { get; } = new[]
    {
        "Ovocné", "Oříškové", "Čokoláda", "Karamel",
        "Květinové", "Kořeněné", "Citrusové", "Med"
    };

    public static IReadOnlyList<string> TeaNoteNames { get; } = new[]
    {
        "Květinové", "Ovocné", "Trávové", "Oříškové",
        "Zemité", "Dřevité", "Kořeněné", "Sladké"
    };
}
