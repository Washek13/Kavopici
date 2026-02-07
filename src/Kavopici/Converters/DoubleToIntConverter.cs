using System.Globalization;
using System.Windows.Data;

namespace Kavopici.Converters;

/// <summary>
/// Converts a double to the nearest int (for star rating display from averages).
/// </summary>
public class DoubleToIntConverter : IValueConverter
{
    public static readonly DoubleToIntConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return (int)Math.Round(d);
        if (value is int i)
            return i;
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int i)
            return (double)i;
        return 0.0;
    }
}
