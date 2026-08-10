using Guardian.ProgramStation.Core.Enums;

namespace Guardian.ProgramStation.Core.Models;

public sealed class ProgramModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public ExecutableType ExecutableType { get; set; } = ExecutableType.Unknown;

    public long Size { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
