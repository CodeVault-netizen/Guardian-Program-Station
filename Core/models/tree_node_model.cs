namespace Guardian.ProgramStation.Core.Models;

public sealed class TreeNodeModel
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Node type: "folder" or "file". Defaults to "folder" for backward compatibility.</summary>
    public string NodeType { get; set; } = "folder";

    /// <summary>Creation time, used by the Creation Time sort options.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TreeNodeModel> Children { get; set; } = new();
}
