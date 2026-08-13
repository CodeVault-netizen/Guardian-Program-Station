using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Guardian.ProgramStation.UI.Services;

/// <summary>Renders the tree's text representation to a PNG image for export.</summary>
public static class TreeImageExporter
{
    public static void SaveTextAsPng(string text, string filePath, int fontSize = 14)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            Brushes.Black);

        var width = Math.Max(1, (int)Math.Ceiling(formatted.Width));
        var height = Math.Max(1, (int)Math.Ceiling(formatted.Height));

        var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        using (var ctx = bitmap.CreateDrawingContext())
        {
            ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            ctx.DrawText(formatted, new Point(0, 0));
        }

        using var stream = File.Create(filePath);
        bitmap.Save(stream);
    }
}
