using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.UseCases;

public sealed class CreateTreeUseCase
{
    private readonly ITreeService _treeService;

    public CreateTreeUseCase(ITreeService treeService)
    {
        _treeService = treeService;
    }

    public Task ExecuteAsync(TreeModel tree, string rootPath, CancellationToken cancellationToken = default)
        => _treeService.CreateFoldersOnDiskAsync(tree, rootPath, cancellationToken);
}
