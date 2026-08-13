namespace Guardian.ProgramStation.Application.Interfaces;

public interface IClipboardService
{
    Task SetTextAsync(string text, CancellationToken cancellationToken = default);

    Task<string?> GetTextAsync(CancellationToken cancellationToken = default);

    Task SetFilesAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetFilesAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
