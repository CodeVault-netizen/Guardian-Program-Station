using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.UseCases;

/// <summary>Result of validating a tree: whether it is valid plus the list of problems found.</summary>
public sealed record TreeValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static readonly TreeValidationResult Valid = new(true, Array.Empty<string>());
}

/// <summary>
/// Validates a <see cref="TreeModel"/> against the same structural rules the
/// GUI relies on (non-empty names, known node types, at least one root node).
/// Shared by the GUI and the CLI so both report identical results.
/// </summary>
public sealed class ValidateTreeUseCase
{
    public TreeValidationResult Execute(TreeModel? tree)
    {
        if (tree is null)
        {
            return new TreeValidationResult(false, new[] { "The tree is null." });
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(tree.Name))
        {
            errors.Add("The tree has no name.");
        }

        if (tree.Nodes.Count == 0)
        {
            errors.Add("The tree has no root nodes.");
        }

        ValidateNodes(tree.Nodes, errors);

        return errors.Count == 0
            ? TreeValidationResult.Valid
            : new TreeValidationResult(false, errors);
    }

    private static void ValidateNodes(IReadOnlyList<TreeNodeModel> nodes, ICollection<string> errors)
    {
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Name))
            {
                errors.Add("A node has an empty name.");
            }

            if (!string.Equals(node.NodeType, "folder", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(node.NodeType, "file", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Node '{node.Name}' has an invalid type '{node.NodeType}'.");
            }

            ValidateNodes(node.Children, errors);
        }
    }
}
