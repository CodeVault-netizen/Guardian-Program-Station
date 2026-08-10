using System.Text.Json;
using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Infrastructure.Helpers;

namespace Guardian.ProgramStation.Infrastructure.Services;

public sealed class SettingsService : ISettingsService
{
    private const string SettingsFileName = "settings.json";

    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _filePath;

    public SettingsService()
    {
        _filePath = Path.Combine(PathHelper.GetSettingsDirectory(), SettingsFileName);
        PathHelper.EnsureDirectories();
    }

    public async Task<SettingsModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new SettingsModel();
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SettingsModel>(json, _options) ?? new SettingsModel();
    }

    public async Task SaveAsync(SettingsModel settings, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(settings, _options);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
    }
}
