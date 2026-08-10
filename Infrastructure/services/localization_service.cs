using System.Text.Json;
using Guardian.ProgramStation.Application.Interfaces;

namespace Guardian.ProgramStation.Infrastructure.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string LanguagesDirectory = "Resources/Languages";
    private const string DefaultLanguage = "en";

    private readonly Dictionary<string, Dictionary<string, string>> _catalog = new();
    private string _currentLanguage = DefaultLanguage;

    public LocalizationService()
    {
        LoadLanguage(DefaultLanguage);
    }

    public event EventHandler? LanguageChanged;

    public string CurrentLanguage => _currentLanguage;

    public bool IsRtl => _currentLanguage == "ar";

    public string this[string key]
        => _catalog.TryGetValue(_currentLanguage, out var language)
           && language.TryGetValue(key, out var value)
               ? value
               : key;

    public Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        if (string.Equals(_currentLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (!_catalog.ContainsKey(languageCode))
        {
            LoadLanguage(languageCode);
        }

        _currentLanguage = languageCode;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private void LoadLanguage(string languageCode)
    {
        try
        {
            var entries = ReadCatalog(languageCode);
            if (entries is not null)
            {
                _catalog[languageCode] = entries;
            }
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }
    }

    private static Dictionary<string, string>? ReadCatalog(string languageCode)
    {
        var path = GetLanguageFilePath(languageCode);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    private static string GetLanguageFilePath(string languageCode)
        => Path.Combine(AppContext.BaseDirectory, LanguagesDirectory, $"{languageCode}.json");
}
