using System.Text;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.UseCases;

/// <summary>
/// Renders a <see cref="TreeModel"/> as terminal-friendly ASCII text. This is
/// the single shared preview implementation used by both the GUI and the CLI,
/// so their output is always identical.
/// </summary>
public sealed class PreviewTreeUseCase
{
    public string Execute(TreeModel tree)
    {
        var builder = new StringBuilder();
        AppendNodes(builder, tree.Nodes, "");
        return builder.ToString().TrimEnd();
    }

    private static void AppendNodes(StringBuilder builder, IReadOnlyList<TreeNodeModel> nodes, string indent)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var isLast = i == nodes.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            builder.Append(indent).Append(connector).AppendLine(nodes[i].Name);

            var childIndent = indent + (isLast ? "    " : "│   ");
            AppendNodes(builder, nodes[i].Children, childIndent);
        }
    }
}
