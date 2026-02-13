namespace Kavopici.Models.Enums;

public static class RoastLevelExtensions
{
    /// <summary>
    /// Returns the Czech display name for the roast level.
    /// </summary>
    public static string ToDisplayString(this RoastLevel level) => level switch
    {
        RoastLevel.Light => "Lehké",
        RoastLevel.MediumLight => "Středně lehké",
        RoastLevel.Medium => "Střední",
        RoastLevel.MediumDark => "Středně tmavé",
        RoastLevel.Dark => "Tmavé",
        _ => level.ToString()
    };
}
