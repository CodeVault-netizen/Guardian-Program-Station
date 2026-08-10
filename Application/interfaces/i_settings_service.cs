using Guardian.ProgramStation.Application.Dtos;

namespace Guardian.ProgramStation.Application.Interfaces;

public interface ISettingsService
{
    Task<SettingsModel> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SettingsModel settings, CancellationToken cancellationToken = default);
}
