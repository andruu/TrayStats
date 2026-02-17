using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TrayStats.Helpers;

/// <summary>
/// Converts a percentage (0-100) to a Width value proportional to the parent container.
/// Uses a reference width of ~150px for compact bars.
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 150;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f)
            return Math.Max(0, Math.Min(MaxWidth, f / 100.0 * MaxWidth));
        if (value is double d)
            return Math.Max(0, Math.Min(MaxWidth, d / 100.0 * MaxWidth));
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Same as PercentToWidth but for larger bars (e.g. RAM detail).
/// </summary>
public class PercentToWidthLargeConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 300;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f)
            return Math.Max(0, Math.Min(MaxWidth, f / 100.0 * MaxWidth));
        if (value is double d)
            return Math.Max(0, Math.Min(MaxWidth, d / 100.0 * MaxWidth));
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Formats a sensor value with a unit suffix, showing "N/A" when the value is 0
/// (meaning the sensor isn't available). Pass the format string as the parameter,
/// e.g. "{0:F0}°C" or "{0:F0} RPM".
/// </summary>
public class SensorValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double v = 0;
        if (value is float f) v = f;
        else if (value is double d) v = d;

        if (v == 0)
            return "N/A";

        string fmt = parameter as string ?? "{0:F0}";
        return string.Format(fmt, v);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
