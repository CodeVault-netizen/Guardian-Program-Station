namespace Guardian.ProgramStation.Core.Models;

public sealed class TreeModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public List<TreeNodeModel> Nodes { get; set; } = new();
}
