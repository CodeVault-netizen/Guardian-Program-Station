using Guardian.ProgramStation.Application.Dtos;

namespace Guardian.ProgramStation.Application.Interfaces;

public interface IThemeService
{
    Task<IReadOnlyList<ThemeDefinition>> GetAvailableThemesAsync(CancellationToken cancellationToken = default);

    Task<ThemeDefinition> GetThemeAsync(string themeId, CancellationToken cancellationToken = default);

    Task SaveCustomThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default);
}
