using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.Interfaces;

public interface ITreeService
{
    Task<IReadOnlyList<TreeModel>> LoadTreesAsync(CancellationToken cancellationToken = default);

    Task<TreeModel?> LoadTreeAsync(string treeId, CancellationToken cancellationToken = default);

    Task SaveTreeAsync(TreeModel tree, CancellationToken cancellationToken = default);

    Task DeleteTreeAsync(string treeId, CancellationToken cancellationToken = default);

    Task CreateFoldersOnDiskAsync(TreeModel tree, string rootPath, CancellationToken cancellationToken = default);

    Task<TreeModel> ImportTreeAsync(string rootPath, CancellationToken cancellationToken = default);
}
