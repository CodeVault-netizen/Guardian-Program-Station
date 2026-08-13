using Guardian.ProgramStation.Application.Interfaces;

namespace Guardian.ProgramStation.Infrastructure.Services;

/// <summary>
/// In-memory <see cref="IClipboardService"/> implementation. Kept as a test
/// double and non-UI fallback: it stores text and file paths for the lifetime
/// of the process and never touches the operating system clipboard. The
/// running application uses <see cref="SystemClipboardService"/> instead.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private string? _text;
    private readonly List<string> _files = new();

    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        _text = text;
        return Task.CompletedTask;
    }

    public Task<string?> GetTextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_text);

    public Task SetFilesAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        _files.Clear();
        _files.AddRange(paths);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetFilesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(_files.ToArray());

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _text = null;
        _files.Clear();
        return Task.CompletedTask;
    }
}
