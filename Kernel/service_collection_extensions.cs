using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Infrastructure.Indexing;
using Guardian.ProgramStation.Infrastructure.Scheduling;
using Guardian.ProgramStation.Infrastructure.Services;
using Guardian.ProgramStation.Infrastructure.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.Kernel;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGuardianProgramStation(this IServiceCollection services)
    {
        services.AddSingleton<IIndexingService, FileSystemIndexer>();
        services.AddSingleton<IStorageService, JsonStorageService>();
        services.AddSingleton<ITreeService, TreeStorageService>();
        services.AddSingleton<IThemeService, ThemeManager>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();

        services.AddSingleton<IndexProgramsUseCase>();
        services.AddSingleton<CreateTreeUseCase>();
        services.AddSingleton<ImportTreeUseCase>();
        services.AddSingleton<ExportReportUseCase>();

        services.AddSingleton<IndexingScheduler>();

        return services;
    }
}
