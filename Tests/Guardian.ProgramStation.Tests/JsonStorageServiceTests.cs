using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Services;
using Xunit;

namespace Guardian.ProgramStation.Tests;

public sealed class JsonStorageServiceTests : IDisposable
{
    private readonly string _dataDir;

    public JsonStorageServiceTests()
    {
        _dataDir = TestData.CreateTempDataDirectory();
    }

    public void Dispose() => TestData.Cleanup(_dataDir);

    [Fact]
    public async Task SaveAndLoadProgram_RoundTrips()
    {
        var service = new JsonStorageService();
        var program = new ProgramModel
        {
            Id = "abc123",
            Name = "Notepad",
            Version = "1.0",
            Path = @"C:\Windows\notepad.exe",
            Link = "https://example.com",
            License = "MIT",
            Notes = "Test note",
            ExecutableType = ExecutableType.Windows,
            Size = 2048,
            AddedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await service.SaveProgramAsync(program);

        var programs = await service.LoadProgramsAsync();
        var loaded = Assert.Single(programs);
        Assert.Equal("abc123", loaded.Id);
        Assert.Equal("Notepad", loaded.Name);
        Assert.Equal("1.0", loaded.Version);
        Assert.Equal("https://example.com", loaded.Link);
        Assert.Equal("MIT", loaded.License);
        Assert.Equal("Test note", loaded.Notes);
        Assert.Equal(ExecutableType.Windows, loaded.ExecutableType);
        Assert.Equal(2048, loaded.Size);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), loaded.AddedAt);
    }

    [Fact]
    public async Task DeleteProgram_RemovesFile()
    {
        var service = new JsonStorageService();
        var program = new ProgramModel { Id = "to-delete", Name = "X", Path = @"C:\x.exe" };

        await service.SaveProgramAsync(program);
        Assert.Single(await service.LoadProgramsAsync());

        await service.DeleteProgramAsync("to-delete");
        Assert.Empty(await service.LoadProgramsAsync());
    }

    [Fact]
    public async Task SaveProgram_WithSameId_UpdatesExisting()
    {
        var service = new JsonStorageService();
        var program = new ProgramModel { Id = "same", Name = "Before", Path = @"C:\a.exe" };
        await service.SaveProgramAsync(program);

        program.Name = "After";
        await service.SaveProgramAsync(program);

        var loaded = Assert.Single(await service.LoadProgramsAsync());
        Assert.Equal("After", loaded.Name);
    }
}
