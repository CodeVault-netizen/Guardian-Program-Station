using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.UseCases;

public sealed class IndexProgramsUseCase
{
    private readonly IIndexingService _indexingService;
    private readonly IStorageService _storageService;
    private readonly ISettingsService _settingsService;

    public IndexProgramsUseCase(
        IIndexingService indexingService,
        IStorageService storageService,
        ISettingsService settingsService)
    {
        _indexingService = indexingService;
        _storageService = storageService;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<ProgramModel>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);

        var discovered = await _indexingService.IndexAsync(settings.FavoriteFolders, cancellationToken);

        var existing = await _storageService.LoadProgramsAsync(cancellationToken);
        var existingPaths = new HashSet<string>(existing.Select(p => p.Path), StringComparer.OrdinalIgnoreCase);

        var addedCount = 0;
        foreach (var program in discovered)
        {
            if (existingPaths.Add(program.Path))
            {
                await _storageService.SaveProgramAsync(program, cancellationToken);
                addedCount++;
            }
        }

        settings.LastIndexAt = DateTime.UtcNow;
        settings.LastIndexFoundCount = addedCount;
        await _settingsService.SaveAsync(settings, cancellationToken);

        return discovered;
    }
}
