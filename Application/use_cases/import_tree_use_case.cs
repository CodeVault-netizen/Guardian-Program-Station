using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;

namespace Guardian.ProgramStation.Application.UseCases;

public sealed class ImportTreeUseCase
{
    private readonly ITreeService _treeService;
    private readonly IIndexingService _indexingService;
    private readonly IStorageService _storageService;

    public ImportTreeUseCase(
        ITreeService treeService,
        IIndexingService indexingService,
        IStorageService storageService)
    {
        _treeService = treeService;
        _indexingService = indexingService;
        _storageService = storageService;
    }

    public async Task<ImportTreeResult> ExecuteAsync(string rootPath, bool importFoldersOnly = false, CancellationToken cancellationToken = default)
    {
        var tree = await _treeService.ImportTreeAsync(rootPath, cancellationToken).ConfigureAwait(false);
        await _treeService.SaveTreeAsync(tree, cancellationToken).ConfigureAwait(false);

        var addedPrograms = importFoldersOnly
            ? 0
            : await ImportProgramsAsync(rootPath, cancellationToken).ConfigureAwait(false);

        return new ImportTreeResult
        {
            Tree = tree,
            AddedProgramsCount = addedPrograms,
        };
    }

    private async Task<int> ImportProgramsAsync(string rootPath, CancellationToken cancellationToken)
    {
        var discovered = await _indexingService.IndexAsync(new[] { rootPath }, cancellationToken).ConfigureAwait(false);

        var existing = await _storageService.LoadProgramsAsync(cancellationToken).ConfigureAwait(false);
        var existingPaths = new HashSet<string>(existing.Select(p => p.Path), StringComparer.OrdinalIgnoreCase);

        var addedCount = 0;
        foreach (var program in discovered)
        {
            if (existingPaths.Add(program.Path))
            {
                await _storageService.SaveProgramAsync(program, cancellationToken).ConfigureAwait(false);
                addedCount++;
            }
        }

        return addedCount;
    }
}
