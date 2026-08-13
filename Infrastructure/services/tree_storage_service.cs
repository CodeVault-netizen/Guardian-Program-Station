using System.Text.Json;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Helpers;

namespace Guardian.ProgramStation.Infrastructure.Services;

public sealed class TreeStorageService : ITreeService
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _directory;

    public TreeStorageService()
    {
        _directory = PathHelper.GetTreesDirectory();
        PathHelper.EnsureDirectories();
    }

    public async Task<IReadOnlyList<TreeModel>> LoadTreesAsync(CancellationToken cancellationToken = default)
    {
        var trees = new List<TreeModel>();

        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var tree = JsonSerializer.Deserialize<TreeModel>(json, _options);

            if (tree is not null)
            {
                trees.Add(tree);
            }
        }

        return trees;
    }

    public async Task<TreeModel?> LoadTreeAsync(string treeId, CancellationToken cancellationToken = default)
    {
        var path = GetFilePath(treeId);
        if (!File.Exists(path))
        {
            return null;
        }

        return await LoadTreeFromFileAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TreeModel?> LoadTreeFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<TreeModel>(json, _options);
    }

    public async Task SaveTreeAsync(TreeModel tree, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tree.Id))
        {
            tree.Id = Guid.NewGuid().ToString("N");
        }

        var json = JsonSerializer.Serialize(tree, _options);
        await File.WriteAllTextAsync(GetFilePath(tree.Id), json, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveTreeToFileAsync(TreeModel tree, string? filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            // No location was chosen (the save dialog was cancelled): write nothing.
            return;
        }

        if (string.IsNullOrWhiteSpace(tree.Id))
        {
            tree.Id = Guid.NewGuid().ToString("N");
        }

        var json = JsonSerializer.Serialize(tree, _options);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteTreeAsync(string treeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetFilePath(treeId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task CreateFoldersOnDiskAsync(TreeModel tree, string rootPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        Directory.CreateDirectory(rootPath);

        foreach (var node in tree.Nodes)
        {
            CreateNode(Path.Combine(rootPath, PathHelper.SanitizeFileName(node.Name)), node, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task<TreeModel> ImportTreeAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        var name = Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var tree = new TreeModel { Name = string.IsNullOrWhiteSpace(name) ? rootPath : name };

        if (Directory.Exists(rootPath))
        {
            foreach (var directory in Directory.EnumerateDirectories(rootPath, "*", DirectoryOptions))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tree.Nodes.Add(BuildNode(directory, cancellationToken));
            }
        }

        return Task.FromResult(tree);
    }

    private static TreeNodeModel BuildNode(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var node = new TreeNodeModel { Name = Path.GetFileName(path) };

        foreach (var directory in Directory.EnumerateDirectories(path, "*", DirectoryOptions))
        {
            node.Children.Add(BuildNode(directory, cancellationToken));
        }

        return node;
    }

    private static void CreateNode(string path, TreeNodeModel node, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(node.NodeType, "file", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }

            return;
        }

        Directory.CreateDirectory(path);

        foreach (var child in node.Children)
        {
            CreateNode(Path.Combine(path, PathHelper.SanitizeFileName(child.Name)), child, cancellationToken);
        }
    }

    private string GetFilePath(string treeId) => Path.Combine(_directory, $"{treeId}.json");

    private static readonly EnumerationOptions DirectoryOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
    };
}
