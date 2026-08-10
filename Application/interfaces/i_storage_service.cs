using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.Interfaces;

public interface IStorageService
{
    Task<IReadOnlyList<ProgramModel>> LoadProgramsAsync(CancellationToken cancellationToken = default);

    Task SaveProgramAsync(ProgramModel program, CancellationToken cancellationToken = default);

    Task DeleteProgramAsync(string programId, CancellationToken cancellationToken = default);
}
