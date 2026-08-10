using System.Globalization;
using Avalonia.Data.Converters;
using Guardian.ProgramStation.Core.Enums;

namespace Guardian.ProgramStation.UI.Converters;

public sealed class ExecutableTypeToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            ExecutableType.Windows => "●",
            ExecutableType.Linux => "◆",
            ExecutableType.MacOs => "▲",
            _ => "○",
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
