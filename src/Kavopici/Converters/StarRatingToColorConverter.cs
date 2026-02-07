using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Kavopici.Converters;

public class StarRatingToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush FilledBrush = new(Color.FromRgb(0xD4, 0xA0, 0x17)); // Amber/Gold
    private static readonly SolidColorBrush EmptyBrush = new(Color.FromRgb(0xA5, 0xAA, 0xAF)); // Silver

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool filled)
            return filled ? FilledBrush : EmptyBrush;
        return EmptyBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
