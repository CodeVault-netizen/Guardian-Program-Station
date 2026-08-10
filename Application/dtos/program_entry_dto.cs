namespace Guardian.ProgramStation.Application.Dtos;

public sealed class ProgramEntryDto
{
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
