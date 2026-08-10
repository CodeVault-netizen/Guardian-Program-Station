using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Services;
using Xunit;

namespace Guardian.ProgramStation.Tests;

public sealed class StatisticsServiceTests
{
    private static ProgramModel Program(string id, string name, ExecutableType type, long size, DateTime addedAt)
        => new()
        {
            Id = id,
            Name = name,
            ExecutableType = type,
            Size = size,
            AddedAt = addedAt,
            Path = $"C:\\programs\\{name}.exe",
        };

    [Fact]
    public async Task GetTotalProgramsAsync_ReturnsCount()
    {
        var programs = new[]
        {
            Program("1", "Alpha", ExecutableType.Windows, 100, DateTime.UtcNow),
            Program("2", "Beta", ExecutableType.Linux, 200, DateTime.UtcNow),
        };
        var service = new StatisticsService(new FakeStorageService(programs), new FakeSettingsService());

        Assert.Equal(2, await service.GetTotalProgramsAsync());
    }

    [Fact]
    public async Task GetTotalSizeAsync_SumsStoredSizes()
    {
        var programs = new[]
        {
            Program("1", "Alpha", ExecutableType.Windows, 1_000, DateTime.UtcNow),
            Program("2", "Beta", ExecutableType.Linux, 2_500, DateTime.UtcNow),
        };
        var service = new StatisticsService(new FakeStorageService(programs), new FakeSettingsService());

        Assert.Equal(3_500, await service.GetTotalSizeAsync());
    }

    [Fact]
    public async Task GetTotalFoldersAsync_ReturnsFavoriteFolderCount()
    {
        var settings = new FakeSettingsService
        {
            Current = new SettingsModel { FavoriteFolders = new List<string> { "A", "B" } },
        };
        var service = new StatisticsService(new FakeStorageService(), settings);

        Assert.Equal(2, await service.GetTotalFoldersAsync());
    }

    [Fact]
    public async Task GetNewProgramsCountAsync_ReturnsLastIndexFoundCount()
    {
        var settings = new FakeSettingsService
        {
            Current = new SettingsModel { LastIndexFoundCount = 7 },
        };
        var service = new StatisticsService(new FakeStorageService(), settings);

        Assert.Equal(7, await service.GetNewProgramsCountAsync());
    }

    [Fact]
    public async Task GetLastUpdateAsync_ReturnsStoredTimestamp()
    {
        var stamp = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var settings = new FakeSettingsService
        {
            Current = new SettingsModel { LastIndexAt = stamp },
        };
        var service = new StatisticsService(new FakeStorageService(), settings);

        Assert.Equal(stamp, await service.GetLastUpdateAsync());
    }

    [Fact]
    public async Task GetRecentProgramsAsync_OrdersByAddedAtDescending()
    {
        var programs = new[]
        {
            Program("1", "Old", ExecutableType.Windows, 0, DateTime.UtcNow.AddDays(-3)),
            Program("2", "New", ExecutableType.Windows, 0, DateTime.UtcNow),
            Program("3", "Mid", ExecutableType.Windows, 0, DateTime.UtcNow.AddDays(-1)),
        };
        var service = new StatisticsService(new FakeStorageService(programs), new FakeSettingsService());

        var recent = await service.GetRecentProgramsAsync(2);

        Assert.Equal(2, recent.Count);
        Assert.Equal("New", recent[0].Name);
        Assert.Equal("Mid", recent[1].Name);
    }

    [Fact]
    public async Task GetProgramsByTypeAsync_ComputesPercentages()
    {
        var programs = new[]
        {
            Program("1", "A", ExecutableType.Windows, 0, DateTime.UtcNow),
            Program("2", "B", ExecutableType.Windows, 0, DateTime.UtcNow),
            Program("3", "C", ExecutableType.Linux, 0, DateTime.UtcNow),
            Program("4", "D", ExecutableType.MacOs, 0, DateTime.UtcNow),
        };
        var service = new StatisticsService(new FakeStorageService(programs), new FakeSettingsService());

        var stats = await service.GetProgramsByTypeAsync();

        var windows = Assert.Single(stats, s => s.Type == ExecutableType.Windows);
        Assert.Equal(2, windows.Count);
        Assert.Equal(50, windows.Percentage);
    }
}
