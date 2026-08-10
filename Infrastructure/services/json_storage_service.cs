using System.Text.Json;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Helpers;

namespace Guardian.ProgramStation.Infrastructure.Services;

public sealed class JsonStorageService : IStorageService
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _directory;

    public JsonStorageService()
    {
        _directory = PathHelper.GetProgramsDirectory();
        PathHelper.EnsureDirectories();
    }

    public async Task<IReadOnlyList<ProgramModel>> LoadProgramsAsync(CancellationToken cancellationToken = default)
    {
        var programs = new List<ProgramModel>();

        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var program = JsonSerializer.Deserialize<ProgramModel>(json, _options);

            if (program is not null)
            {
                programs.Add(program);
            }
        }

        return programs;
    }

    public async Task SaveProgramAsync(ProgramModel program, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(program, _options);
        await File.WriteAllTextAsync(GetFilePath(program.Id), json, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteProgramAsync(string programId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetFilePath(programId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string programId) => Path.Combine(_directory, $"{programId}.json");
}
