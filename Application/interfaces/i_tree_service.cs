using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.Interfaces;

public interface ITreeService
{
    Task<IReadOnlyList<TreeModel>> LoadTreesAsync(CancellationToken cancellationToken = default);

    Task<TreeModel?> LoadTreeAsync(string treeId, CancellationToken cancellationToken = default);

    Task<TreeModel?> LoadTreeFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task SaveTreeAsync(TreeModel tree, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the tree to the given file path. An empty or null path (for
    /// example when the user cancels the save dialog) writes nothing.
    /// </summary>
    Task SaveTreeToFileAsync(TreeModel tree, string? filePath, CancellationToken cancellationToken = default);

    Task DeleteTreeAsync(string treeId, CancellationToken cancellationToken = default);

    Task CreateFoldersOnDiskAsync(TreeModel tree, string rootPath, CancellationToken cancellationToken = default);

    Task<TreeModel> ImportTreeAsync(string rootPath, CancellationToken cancellationToken = default);
}
