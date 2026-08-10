namespace Guardian.ProgramStation.Core.Models;

public sealed class TreeNodeModel
{
    public string Name { get; set; } = string.Empty;

    public List<TreeNodeModel> Children { get; set; } = new();
}
