using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Core.Sorting;
using Xunit;

namespace Guardian.ProgramStation.Tests;

public class TreeNodeSorterTests
{
    private static TreeNodeModel Node(string name, params TreeNodeModel[] children) => new()
    {
        Name = name,
        Children = children.ToList(),
    };

    private static List<string> Names(IEnumerable<TreeNodeModel> nodes) => nodes.Select(n => n.Name).ToList();

    private static List<TreeNodeModel> BuildTree() =>
    [
        Node("root2",
            Node("zebra",
                Node("grand-b"),
                Node("grand-a")),
            Node("apple")),
        Node("root1",
            Node("beta"),
            Node("alpha")),
    ];

    [Fact]
    public void Sort_NameAscending_SortsEveryLevelRecursively()
    {
        var roots = BuildTree();

        TreeNodeSorter.Sort(roots, TreeNodeSortOption.NameAscending);

        Assert.Equal(new[] { "root1", "root2" }, Names(roots));
        Assert.Equal(new[] { "alpha", "beta" }, Names(roots[0].Children));
        Assert.Equal(new[] { "apple", "zebra" }, Names(roots[1].Children));
        Assert.Equal(new[] { "grand-a", "grand-b" }, Names(roots[1].Children[1].Children));
    }

    [Fact]
    public void Sort_NameDescending_SortsEveryLevelRecursively()
    {
        var roots = BuildTree();

        TreeNodeSorter.Sort(roots, TreeNodeSortOption.NameDescending);

        Assert.Equal(new[] { "root2", "root1" }, Names(roots));
        Assert.Equal(new[] { "zebra", "apple" }, Names(roots[0].Children));
        Assert.Equal(new[] { "grand-b", "grand-a" }, Names(roots[0].Children[0].Children));
        Assert.Equal(new[] { "beta", "alpha" }, Names(roots[1].Children));
    }

    [Fact]
    public void Sort_CreationOldestFirst_SortsEveryLevelRecursively()
    {
        var roots = new List<TreeNodeModel>
        {
            Node("newest", Node("child-newest"), Node("child-oldest")),
            Node("oldest"),
        };

        roots[0].CreatedAt = new DateTime(2024, 3, 1);
        roots[0].Children[0].CreatedAt = new DateTime(2024, 2, 1);
        roots[0].Children[1].CreatedAt = new DateTime(2023, 1, 1);
        roots[1].CreatedAt = new DateTime(2020, 1, 1);

        TreeNodeSorter.Sort(roots, TreeNodeSortOption.CreationOldestFirst);

        Assert.Equal(new[] { "oldest", "newest" }, Names(roots));
        Assert.Equal(new[] { "child-oldest", "child-newest" }, Names(roots[1].Children));
    }

    [Fact]
    public void Sort_CreationNewestFirst_SortsEveryLevelRecursively()
    {
        var roots = new List<TreeNodeModel>
        {
            Node("oldest", Node("child-oldest"), Node("child-newest")),
            Node("newest"),
        };

        roots[0].CreatedAt = new DateTime(2020, 1, 1);
        roots[0].Children[0].CreatedAt = new DateTime(2023, 1, 1);
        roots[0].Children[1].CreatedAt = new DateTime(2024, 2, 1);
        roots[1].CreatedAt = new DateTime(2024, 3, 1);

        TreeNodeSorter.Sort(roots, TreeNodeSortOption.CreationNewestFirst);

        Assert.Equal(new[] { "newest", "oldest" }, Names(roots));
        Assert.Equal(new[] { "child-newest", "child-oldest" }, Names(roots[1].Children));
    }

    [Fact]
    public void Sort_PreservesNodesAndParentChildRelations()
    {
        var roots = BuildTree();
        var before = Flatten(roots);
        var beforeNames = before.Select(n => n.Name).OrderBy(n => n).ToList();

        TreeNodeSorter.Sort(roots, TreeNodeSortOption.NameAscending);

        var after = Flatten(roots);
        Assert.Equal(before.Count, after.Count);
        Assert.Equal(beforeNames, after.Select(n => n.Name).OrderBy(n => n).ToList());

        // Each node keeps exactly the same children, only reordered.
        foreach (var original in before)
        {
            var moved = after.Single(n => ReferenceEquals(n, original));
            Assert.Equal(original.Children.Select(c => c.Name).OrderBy(n => n).ToList(),
                         moved.Children.Select(c => c.Name).OrderBy(n => n).ToList());
        }
    }

    private static List<TreeNodeModel> Flatten(IEnumerable<TreeNodeModel> nodes)
        => nodes.SelectMany(n => new[] { n }.Concat(Flatten(n.Children))).ToList();
}
