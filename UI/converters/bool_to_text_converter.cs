using System.Globalization;
using Avalonia.Data.Converters;

namespace Guardian.ProgramStation.UI.Converters;

/// <summary>Returns one of two texts (separated by '|' in the parameter) based on the boolean value.</summary>
public sealed class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var pair = parameter?.ToString()?.Split('|');
        if (pair is not { Length: 2 })
        {
            return value is true ? "✓" : "○";
        }

        return value is true ? pair[0] : pair[1];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
