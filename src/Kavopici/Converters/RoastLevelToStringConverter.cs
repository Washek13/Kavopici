using System.Globalization;
using System.Windows.Data;
using Kavopici.Models.Enums;

namespace Kavopici.Converters;

public class RoastLevelToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RoastLevel level)
        {
            return level switch
            {
                RoastLevel.Light => "Lehké",
                RoastLevel.MediumLight => "Středně lehké",
                RoastLevel.Medium => "Střední",
                RoastLevel.MediumDark => "Středně tmavé",
                RoastLevel.Dark => "Tmavé",
                _ => level.ToString()
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
