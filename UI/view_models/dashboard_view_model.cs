using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Avalonia.Media;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IStatisticsService _statistics;
    private readonly ILocalizationService _localization;
    private string _lastUpdateText = "--";

    public DashboardViewModel(IServiceProvider services, ILocalizationService localization)
    {
        _statistics = services.GetRequiredService<IStatisticsService>();
        _localization = localization;

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());
    }

    public ICommand RefreshCommand { get; }

    public ObservableCollection<TypeStatItem> TypeStats { get; } = new();

    public ObservableCollection<RecentProgramItem> RecentPrograms { get; } = new();

    public int TotalPrograms { get; private set; }

    public string TotalProgramsText => TotalPrograms.ToString("N0", CultureInfo.InvariantCulture);

    public string TotalSizeText { get; private set; } = "0";

    public int TotalFolders { get; private set; }

    public string LastUpdateText => _lastUpdateText;

    public int NewProgramsCount { get; private set; }

    public string NewProgramsText => NewProgramsCount.ToString("N0", CultureInfo.InvariantCulture);

    public string DashboardLabel => _localization["Dashboard"];

    public string TotalProgramsLabel => _localization["TotalPrograms"];

    public string TotalSizeLabel => _localization["TotalSize"];

    public string TotalFoldersLabel => _localization["TotalFolders"];

    public string LastUpdateLabel => _localization["LastUpdate"];

    public string NewProgramsLabel => _localization["NewPrograms"];

    public string TypeDistributionLabel => _localization["TypeDistribution"];

    public string RecentProgramsLabel => _localization["RecentPrograms"];

    public string RefreshLabel => _localization["Refresh"];

    public FlowDirection Direction => _localization.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public async Task LoadAsync()
    {
        TotalPrograms = await _statistics.GetTotalProgramsAsync();
        OnPropertyChanged(nameof(TotalPrograms));
        OnPropertyChanged(nameof(TotalProgramsText));

        TotalSizeText = FormatSize(await _statistics.GetTotalSizeAsync());
        OnPropertyChanged(nameof(TotalSizeText));

        TotalFolders = await _statistics.GetTotalFoldersAsync();
        OnPropertyChanged(nameof(TotalFolders));

        var lastUpdate = await _statistics.GetLastUpdateAsync();
        _lastUpdateText = lastUpdate.HasValue
            ? lastUpdate.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)
            : _localization["Never"];
        OnPropertyChanged(nameof(LastUpdateText));

        NewProgramsCount = await _statistics.GetNewProgramsCountAsync();
        OnPropertyChanged(nameof(NewProgramsCount));
        OnPropertyChanged(nameof(NewProgramsText));

        await LoadTypeStatsAsync();
        await LoadRecentProgramsAsync();
    }

    public void RefreshLocalized()
    {
        OnPropertyChanged(nameof(DashboardLabel));
        OnPropertyChanged(nameof(TotalProgramsLabel));
        OnPropertyChanged(nameof(TotalSizeLabel));
        OnPropertyChanged(nameof(TotalFoldersLabel));
        OnPropertyChanged(nameof(LastUpdateLabel));
        OnPropertyChanged(nameof(NewProgramsLabel));
        OnPropertyChanged(nameof(TypeDistributionLabel));
        OnPropertyChanged(nameof(RecentProgramsLabel));
        OnPropertyChanged(nameof(RefreshLabel));
        OnPropertyChanged(nameof(Direction));

        _ = LoadAsync();
    }

    private async Task LoadTypeStatsAsync()
    {
        var stats = await _statistics.GetProgramsByTypeAsync();

        TypeStats.Clear();
        foreach (var stat in stats)
        {
            TypeStats.Add(new TypeStatItem
            {
                Name = GetTypeName(stat.Type),
                Icon = GetTypeIcon(stat.Type),
                Count = stat.Count,
                PercentageText = stat.Percentage.ToString("0.#", CultureInfo.CurrentCulture) + "%",
            });
        }
    }

    private async Task LoadRecentProgramsAsync()
    {
        var programs = await _statistics.GetRecentProgramsAsync(5);

        RecentPrograms.Clear();
        foreach (var program in programs)
        {
            RecentPrograms.Add(new RecentProgramItem
            {
                Name = program.Name,
                Version = program.Version,
                AddedAtText = program.AddedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture),
            });
        }
    }

    private string GetTypeName(ExecutableType type) => type switch
    {
        ExecutableType.Windows => _localization["Windows"],
        ExecutableType.Linux => _localization["Linux"],
        ExecutableType.MacOs => _localization["MacOS"],
        _ => _localization["Unknown"],
    };

    private static string GetTypeIcon(ExecutableType type) => type switch
    {
        ExecutableType.Windows => "●",
        ExecutableType.Linux => "◆",
        ExecutableType.MacOs => "▲",
        _ => "○",
    };

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
        {
            return (bytes / (1024.0 * 1024 * 1024)).ToString("0.##", CultureInfo.CurrentCulture) + " GB";
        }

        if (bytes >= 1024L * 1024)
        {
            return (bytes / (1024.0 * 1024)).ToString("0.##", CultureInfo.CurrentCulture) + " MB";
        }

        if (bytes >= 1024)
        {
            return (bytes / 1024.0).ToString("0.##", CultureInfo.CurrentCulture) + " KB";
        }

        return bytes.ToString("N0", CultureInfo.InvariantCulture) + " B";
    }
}

public sealed class TypeStatItem
{
    public string Name { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public int Count { get; init; }

    public string PercentageText { get; init; } = string.Empty;
}

public sealed class RecentProgramItem
{
    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string AddedAtText { get; init; } = string.Empty;
}
