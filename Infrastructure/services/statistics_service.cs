using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Infrastructure.Services;

public sealed class StatisticsService : IStatisticsService
{
    private readonly IStorageService _storageService;
    private readonly ISettingsService _settingsService;

    public StatisticsService(IStorageService storageService, ISettingsService settingsService)
    {
        _storageService = storageService;
        _settingsService = settingsService;
    }

    public async Task<int> GetTotalProgramsAsync(CancellationToken cancellationToken = default)
    {
        var programs = await _storageService.LoadProgramsAsync(cancellationToken).ConfigureAwait(false);
        return programs.Count;
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        var programs = await _storageService.LoadProgramsAsync(cancellationToken).ConfigureAwait(false);

        long total = 0;
        foreach (var program in programs)
        {
            total += ResolveSize(program);
        }

        return total;
    }

    public async Task<int> GetTotalFoldersAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return settings.FavoriteFolders.Count;
    }

    public async Task<DateTime?> GetLastUpdateAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return settings.LastIndexAt;
    }

    public async Task<int> GetNewProgramsCountAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return settings.LastIndexFoundCount;
    }

    public async Task<IReadOnlyList<ProgramModel>> GetRecentProgramsAsync(int count, CancellationToken cancellationToken = default)
    {
        var programs = await _storageService.LoadProgramsAsync(cancellationToken).ConfigureAwait(false);

        return programs
            .OrderByDescending(p => p.AddedAt)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyList<TypeStatistic>> GetProgramsByTypeAsync(CancellationToken cancellationToken = default)
    {
        var programs = await _storageService.LoadProgramsAsync(cancellationToken).ConfigureAwait(false);
        var total = programs.Count;

        return programs
            .GroupBy(p => p.ExecutableType)
            .OrderByDescending(g => g.Count())
            .Select(g => new TypeStatistic
            {
                Type = g.Key,
                Count = g.Count(),
                Percentage = total == 0 ? 0 : Math.Round(g.Count() * 100.0 / total, 1),
            })
            .ToList();
    }

    private static long ResolveSize(ProgramModel program)
    {
        if (program.Size > 0)
        {
            return program.Size;
        }

        try
        {
            var info = new FileInfo(program.Path);
            return info.Exists ? info.Length : 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
