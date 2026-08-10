using System.Text.Json;
using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Infrastructure.Helpers;

namespace Guardian.ProgramStation.Infrastructure.Themes;

public sealed class ThemeManager : IThemeService
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _directory;

    public ThemeManager()
    {
        _directory = PathHelper.GetThemesDirectory();
        PathHelper.EnsureDirectories();
    }

    public Task<IReadOnlyList<ThemeDefinition>> GetAvailableThemesAsync(CancellationToken cancellationToken = default)
    {
        var themes = new List<ThemeDefinition>
        {
            CreateDarkTheme(),
            CreateLightTheme(),
        };

        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var json = File.ReadAllText(file);
                var theme = JsonSerializer.Deserialize<ThemeDefinition>(json, _options);

                if (theme is not null)
                {
                    theme.IsBuiltIn = false;
                    themes.Add(theme);
                }
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }
        }

        return Task.FromResult<IReadOnlyList<ThemeDefinition>>(themes);
    }

    public async Task<ThemeDefinition> GetThemeAsync(string themeId, CancellationToken cancellationToken = default)
    {
        var themes = await GetAvailableThemesAsync(cancellationToken).ConfigureAwait(false);
        return themes.FirstOrDefault(t => t.Id == themeId) ?? CreateDarkTheme();
    }

    public async Task SaveCustomThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(theme.Id))
        {
            theme.Id = Guid.NewGuid().ToString("N");
        }

        theme.IsBuiltIn = false;

        var json = JsonSerializer.Serialize(theme, _options);
        await File.WriteAllTextAsync(Path.Combine(_directory, $"{theme.Id}.json"), json, cancellationToken).ConfigureAwait(false);
    }

    private static ThemeDefinition CreateDarkTheme() => new()
    {
        Id = "dark",
        Name = "Dark",
        IsBuiltIn = true,
        Palette = new ThemePalette
        {
            Background = "#2C2A29",
            ElementBackground = "#4B4A48",
            AdditionalBackground = "#3A3837",
            Border = "#7A7B7A",
            PrimaryText = "#A8A8A8",
            AccentText = "#D3D3D3",
        },
    };

    private static ThemeDefinition CreateLightTheme() => new()
    {
        Id = "light",
        Name = "Light",
        IsBuiltIn = true,
        Palette = new ThemePalette
        {
            Background = "#EAD7BB",
            ElementBackground = "#EBE3D5",
            AdditionalBackground = "#F3EEEA",
            Border = "#D9C9AC",
            PrimaryText = "#776B5D",
            AccentText = "#B0A695",
        },
    };
}
