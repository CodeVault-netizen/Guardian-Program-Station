using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Core.Sorting;

/// <summary>Sorts a tree of <see cref="TreeNodeModel"/> recursively, reordering siblings only — nodes are never added, removed, renamed, or reparented.</summary>
public static class TreeNodeSorter
{
    public static void Sort(IList<TreeNodeModel> nodes, TreeNodeSortOption option)
    {
        // Sort every level below first, so the whole tree is sorted recursively.
        foreach (var node in nodes)
        {
            Sort(node.Children, option);
        }

        var ordered = option switch
        {
            TreeNodeSortOption.NameAscending => nodes.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase),
            TreeNodeSortOption.NameDescending => nodes.OrderByDescending(n => n.Name, StringComparer.OrdinalIgnoreCase),
            TreeNodeSortOption.CreationOldestFirst => nodes.OrderBy(n => n.CreatedAt),
            TreeNodeSortOption.CreationNewestFirst => nodes.OrderByDescending(n => n.CreatedAt),
            _ => nodes.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase),
        };

        var sorted = ordered.ToList();
        nodes.Clear();
        foreach (var node in sorted)
        {
            nodes.Add(node);
        }
    }
}
