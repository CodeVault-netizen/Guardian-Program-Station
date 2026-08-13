using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Guardian.ProgramStation.UI.Converters;

/// <summary>Returns an alternate-row brush when the bound value is true, otherwise transparent.</summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new SolidColorBrush(Color.Parse("#333333")) : Brushes.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
