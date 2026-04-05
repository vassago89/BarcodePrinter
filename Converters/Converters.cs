using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BarcodePrinter.Converters;

public class BoolToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Connected = new(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly SolidColorBrush Disconnected = new(Color.FromRgb(0xEF, 0x44, 0x44));

    static BoolToColorConverter()
    {
        Connected.Freeze();
        Disconnected.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Connected : Disconnected;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ResultToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush OkBrush = new(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly SolidColorBrush NgBrush = new(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly SolidColorBrush DefaultBrush = new(Colors.Transparent);

    static ResultToColorConverter()
    {
        OkBrush.Freeze();
        NgBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() switch
        {
            "OK" => OkBrush,
            "NG" => NgBrush,
            _ => DefaultBrush
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
