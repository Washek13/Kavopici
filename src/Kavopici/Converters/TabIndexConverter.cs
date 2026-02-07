using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Kavopici.Converters;

/// <summary>
/// Converts between tab index (int) and boolean for RadioButton IsChecked binding.
/// ConverterParameter is the target tab index as a string.
/// </summary>
public class TabIndexConverter : IValueConverter
{
    public static readonly TabIndexConverter Instance = new();
    public static readonly TabIndexToVisibilityConverter VisibilityInstance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int tabIndex && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return tabIndex == targetIndex;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return targetIndex;
        }
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts tab index to Visibility. ConverterParameter is the target tab index.
/// </summary>
public class TabIndexToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int tabIndex && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return tabIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
