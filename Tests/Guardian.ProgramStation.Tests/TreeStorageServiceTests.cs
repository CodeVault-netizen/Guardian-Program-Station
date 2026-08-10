using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Services;
using Xunit;

namespace Guardian.ProgramStation.Tests;

public sealed class TreeStorageServiceTests : IDisposable
{
    private readonly string _dataDir;

    public TreeStorageServiceTests()
    {
        _dataDir = TestData.CreateTempDataDirectory();
    }

    public void Dispose() => TestData.Cleanup(_dataDir);

    [Fact]
    public async Task SaveTreeAsync_AssignsId_AndPersistsFile()
    {
        var service = new TreeStorageService();
        var tree = new TreeModel { Name = "Project" };
        tree.Nodes.Add(new TreeNodeModel { Name = "src" });

        await service.SaveTreeAsync(tree);

        Assert.False(string.IsNullOrWhiteSpace(tree.Id));

        var loaded = await service.LoadTreeAsync(tree.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Project", loaded!.Name);
        Assert.Single(loaded.Nodes);
        Assert.Equal("src", loaded.Nodes[0].Name);
    }

    [Fact]
    public async Task LoadTreesAsync_ReturnsAllSavedTrees()
    {
        var service = new TreeStorageService();
        await service.SaveTreeAsync(new TreeModel { Name = "A" });
        await service.SaveTreeAsync(new TreeModel { Name = "B" });

        var trees = await service.LoadTreesAsync();

        Assert.Equal(2, trees.Count);
    }

    [Fact]
    public async Task DeleteTreeAsync_RemovesPersistedTree()
    {
        var service = new TreeStorageService();
        var tree = new TreeModel { Name = "A" };
        await service.SaveTreeAsync(tree);

        await service.DeleteTreeAsync(tree.Id);

        Assert.Null(await service.LoadTreeAsync(tree.Id));
        Assert.Empty(await service.LoadTreesAsync());
    }

    [Fact]
    public async Task CreateFoldersOnDiskAsync_CreatesNestedStructure()
    {
        var service = new TreeStorageService();
        var root = Path.Combine(_dataDir, "created", "root");
        var tree = new TreeModel
        {
            Name = "T",
            Nodes = new List<TreeNodeModel>
            {
                new() { Name = "src", Children = new List<TreeNodeModel> { new() { Name = "features" } } },
                new() { Name = "docs" },
            },
        };

        await service.CreateFoldersOnDiskAsync(tree, root);

        Assert.True(Directory.Exists(Path.Combine(root, "src", "features")));
        Assert.True(Directory.Exists(Path.Combine(root, "docs")));
    }

    [Fact]
    public async Task ImportTreeAsync_BuildsTreeFromDirectory()
    {
        var service = new TreeStorageService();
        var source = Path.Combine(_dataDir, "import-source");
        Directory.CreateDirectory(Path.Combine(source, "src", "deep"));
        Directory.CreateDirectory(Path.Combine(source, "docs"));

        var tree = await service.ImportTreeAsync(source);

        Assert.Equal("import-source", tree.Name);
        Assert.Equal(2, tree.Nodes.Count);

        var src = Assert.Single(tree.Nodes, n => n.Name == "src");
        Assert.Single(src.Children);
        Assert.Equal("deep", src.Children[0].Name);
    }
}
