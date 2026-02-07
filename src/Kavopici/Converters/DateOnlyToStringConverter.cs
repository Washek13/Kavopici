using System.Globalization;
using System.Windows.Data;

namespace Kavopici.Converters;

public class DateOnlyToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateOnly date)
            return date.ToString("d. M. yyyy");
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
