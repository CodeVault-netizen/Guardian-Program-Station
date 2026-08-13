using System.Windows.Input;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Services;

namespace Guardian.ProgramStation.UI.ViewModels;

/// <summary>Shared helpers for copying and pasting tree nodes between view models.</summary>
internal static class TreeNodeClipboard
{
    public static string Serialize(TreeNodeViewModel node)
        => TreeClipboardFormat.Serialize(ToModel(node));

    public static TreeNodeViewModel CreateNode(ITreeNodeHost host, TreeNodeModel model, TreeNodeViewModel? parent)
    {
        var viewModel = new TreeNodeViewModel(host)
        {
            Parent = parent,
            Name = model.Name,
            NodeType = model.NodeType,
            CreatedAt = model.CreatedAt,
        };

        foreach (var child in model.Children)
        {
            viewModel.Children.Add(CreateNode(host, child, viewModel));
        }

        return viewModel;
    }

    private static TreeNodeModel ToModel(TreeNodeViewModel node) => new()
    {
        Name = node.Name,
        NodeType = node.NodeType,
        CreatedAt = node.CreatedAt,
        Children = node.Children.Select(ToModel).ToList(),
    };
}

/// <summary>
/// Implements the Copy / Cut / Paste / Delete commands shared by the tree
/// editors against the real system clipboard (<see cref="IClipboardService"/>).
/// Copied nodes are serialized with <see cref="TreeClipboardFormat"/>; paste
/// rebuilds the node, or falls back to importing file paths or plain text when
/// the clipboard holds foreign content.
/// </summary>
internal sealed class TreeNodeClipboardController
{
    private readonly ITreeNodeHost _host;
    private readonly IClipboardService _clipboard;

    public TreeNodeClipboardController(ITreeNodeHost host, IClipboardService clipboard)
    {
        _host = host;
        _clipboard = clipboard;

        CopyCommand = new AsyncRelayCommand(async parameter => await CopyAsync(parameter as TreeNodeViewModel));
        CutCommand = new AsyncRelayCommand(async parameter => await CutAsync(parameter as TreeNodeViewModel));
        PasteCommand = new AsyncRelayCommand(async parameter => await PasteAsync(parameter as TreeNodeViewModel));
        CopyNameCommand = new AsyncRelayCommand(async parameter => await CopyNameAsync(parameter as TreeNodeViewModel));
        PasteNameCommand = new AsyncRelayCommand(async parameter => await PasteNameAsync(parameter as TreeNodeViewModel));
    }

    public ICommand CopyCommand { get; }

    public ICommand CutCommand { get; }

    public ICommand PasteCommand { get; }

    public ICommand DeleteCommand => _host.RemoveNodeCommand;

    /// <summary>Copies only the node's name as plain text (no serialized JSON).</summary>
    public ICommand CopyNameCommand { get; }

    /// <summary>
    /// Pastes the clipboard's plain text as the node's name. If the clipboard
    /// holds this app's serialized node format, only the name is extracted.
    /// </summary>
    public ICommand PasteNameCommand { get; }

    private async Task CopyAsync(TreeNodeViewModel? node)
    {
        if (node is null)
        {
            return;
        }

        await _clipboard.SetTextAsync(TreeNodeClipboard.Serialize(node));
    }

    private async Task CopyNameAsync(TreeNodeViewModel? node)
    {
        if (node is null || string.IsNullOrWhiteSpace(node.Name))
        {
            return;
        }

        await _clipboard.SetTextAsync(node.Name.Trim());
    }

    private async Task PasteNameAsync(TreeNodeViewModel? node)
    {
        if (node is null)
        {
            return;
        }

        var text = await _clipboard.GetTextAsync();
        var name = TreeClipboardFormat.ExtractName(text);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        node.Name = name;
    }

    private async Task CutAsync(TreeNodeViewModel? node)
    {
        if (node is null)
        {
            return;
        }

        await CopyAsync(node);
        _host.RemoveNodeCommand.Execute(node);
    }

    private async Task PasteAsync(TreeNodeViewModel? target)
    {
        if (target is null)
        {
            return;
        }

        // 1) Our own format: rebuild the copied node (including its subtree).
        var text = await _clipboard.GetTextAsync();
        if (!string.IsNullOrEmpty(text))
        {
            var model = TreeClipboardFormat.TryDeserialize(text);
            if (model is not null)
            {
                var copy = TreeNodeClipboard.CreateNode(_host, model, target);
                copy.IsExpanded = true;
                target.Children.Add(copy);
                return;
            }
        }

        // 2) Files copied in the operating system (e.g. from Explorer).
        var paths = await _clipboard.GetFilesAsync();
        if (paths.Count > 0)
        {
            foreach (var path in paths)
            {
                var name = Path.GetFileName(path);
                if (name.Length == 0)
                {
                    continue;
                }

                target.Children.Add(new TreeNodeViewModel(_host)
                {
                    Parent = target,
                    Name = name,
                    NodeType = Directory.Exists(path) ? "folder" : "file",
                });
            }

            return;
        }

        // 3) Plain text fallback: import the text as a file node.
        var trimmed = text?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            target.Children.Add(new TreeNodeViewModel(_host)
            {
                Parent = target,
                Name = trimmed,
                NodeType = "file",
            });
        }
    }
}
