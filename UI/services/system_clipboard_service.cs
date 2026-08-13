using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Guardian.ProgramStation.Application.Interfaces;

namespace Guardian.ProgramStation.UI.Services;

/// <summary>
/// <see cref="IClipboardService"/> backed by the real Windows system clipboard
/// through Avalonia's <c>TopLevel.Clipboard</c> API.
/// </summary>
public sealed class SystemClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var clipboard = GetPlatformClipboard();
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public async Task<string?> GetTextAsync(CancellationToken cancellationToken = default)
        => GetPlatformClipboard() is { } clipboard ? await clipboard.TryGetTextAsync() : null;

    public async Task SetFilesAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        var clipboard = GetPlatformClipboard();
        if (clipboard is null || paths.Count == 0)
        {
            return;
        }

        var files = new List<IStorageItem>(paths.Count);
        foreach (var path in paths)
        {
            var item = await ToStorageItemAsync(path);
            if (item is not null)
            {
                files.Add(item);
            }
        }

        if (files.Count > 0)
        {
            await clipboard.SetFilesAsync(files);
        }
    }

    public async Task<IReadOnlyList<string>> GetFilesAsync(CancellationToken cancellationToken = default)
    {
        var clipboard = GetPlatformClipboard();
        if (clipboard is null)
        {
            return Array.Empty<string>();
        }

        var items = await clipboard.TryGetFilesAsync();
        if (items is null || items.Length == 0)
        {
            return Array.Empty<string>();
        }

        return items
            .Select(item => item.TryGetLocalPath())
            .Where(path => path is not null)
            .Cast<string>()
            .ToArray();
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (GetPlatformClipboard() is { } clipboard)
        {
            await clipboard.ClearAsync();
        }
    }

    private static IClipboard? GetPlatformClipboard()
        => Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
           {
               MainWindow: { } main
           }
               ? main.Clipboard
               : null;

    private static async Task<IStorageItem?> ToStorageItemAsync(string path)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: { } main
            })
        {
            return null;
        }

        var storage = main.StorageProvider;
        var file = await storage.TryGetFileFromPathAsync(path);
        if (file is not null)
        {
            return file;
        }

        return await storage.TryGetFolderFromPathAsync(path);
    }
}
