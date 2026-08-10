using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.Interfaces;

public interface IIndexingService
{
    Task<IReadOnlyList<ProgramModel>> IndexAsync(IEnumerable<string> folderPaths, CancellationToken cancellationToken = default);
}
