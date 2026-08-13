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

    /// <summary>The parent folder/section of the executable, used to group the report.</summary>
    public string ParentSection
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                return "Other";
            }

            var normalized = Path.Replace('/', System.IO.Path.DirectorySeparatorChar)
                                 .Replace('\\', System.IO.Path.DirectorySeparatorChar);
            var dir = System.IO.Path.GetDirectoryName(normalized);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return "Other";
            }

            var name = System.IO.Path.GetFileName(dir.TrimEnd(System.IO.Path.DirectorySeparatorChar));
            return string.IsNullOrWhiteSpace(name) ? "Other" : name;
        }
    }

    public ExecutableType ExecutableType { get; set; } = ExecutableType.Unknown;

    /// <summary>Set by the report view model to alternate row backgrounds within a group.</summary>
    public bool IsAlternate { get; set; }

    public long Size { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
