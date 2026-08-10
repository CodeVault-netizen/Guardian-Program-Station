using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.Interfaces;

public interface IStatisticsService
{
    Task<int> GetTotalProgramsAsync(CancellationToken cancellationToken = default);

    Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default);

    Task<int> GetTotalFoldersAsync(CancellationToken cancellationToken = default);

    Task<DateTime?> GetLastUpdateAsync(CancellationToken cancellationToken = default);

    Task<int> GetNewProgramsCountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProgramModel>> GetRecentProgramsAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TypeStatistic>> GetProgramsByTypeAsync(CancellationToken cancellationToken = default);
}
