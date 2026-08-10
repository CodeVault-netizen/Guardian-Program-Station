namespace Guardian.ProgramStation.Application.Interfaces;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;

    string CurrentLanguage { get; }

    bool IsRtl { get; }

    string this[string key] { get; }

    Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default);
}
