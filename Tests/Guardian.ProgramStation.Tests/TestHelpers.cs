using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Guardian.ProgramStation.Tests;

internal sealed class FakeStorageService : IStorageService
{
    private readonly List<ProgramModel> _programs = new();

    public FakeStorageService(IEnumerable<ProgramModel>? programs = null)
    {
        if (programs is not null)
        {
            _programs.AddRange(programs);
        }
    }

    public Task<IReadOnlyList<ProgramModel>> LoadProgramsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProgramModel>>(_programs.ToList());

    public Task SaveProgramAsync(ProgramModel program, CancellationToken cancellationToken = default)
    {
        var index = _programs.FindIndex(p => p.Id == program.Id);
        if (index >= 0)
        {
            _programs[index] = program;
        }
        else
        {
            _programs.Add(program);
        }

        return Task.CompletedTask;
    }

    public Task DeleteProgramAsync(string programId, CancellationToken cancellationToken = default)
    {
        _programs.RemoveAll(p => p.Id == programId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeSettingsService : ISettingsService
{
    public SettingsModel Current { get; set; } = new();

    public Task<SettingsModel> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Current);

    public Task SaveAsync(SettingsModel settings, CancellationToken cancellationToken = default)
    {
        Current = settings;
        return Task.CompletedTask;
    }
}

internal static class TestData
{
    public static string CreateTempDataDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "gps-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        Environment.SetEnvironmentVariable("GUARDIAN_PROGRAM_STATION_DATA", path);
        return path;
    }

    public static void Cleanup(string path)
    {
        Environment.SetEnvironmentVariable("GUARDIAN_PROGRAM_STATION_DATA", null);

        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
