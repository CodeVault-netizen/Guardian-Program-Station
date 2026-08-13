using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.UI.Views;

namespace Guardian.ProgramStation.UI.ViewModels;

/// <summary>Result of the new-tree dialog: the saved tree plus the location the user chose.</summary>
public sealed record TreeEditorResult(TreeModel Tree, string? SavePath);

public sealed class TreeEditorViewModel : ObservableObject, ITreeNodeHost
{
    private readonly ITreeService _treeService;
    private readonly ILocalizationService _localization;
    private readonly TreeNodeClipboardController _clipboardController;
    private string _treeName = string.Empty;
    private TreeNodeViewModel? _selectedNode;
    private TreeNodeViewModel? _contextMenuNode;
    private bool _isNodeContextMenuOpen;

    public TreeEditorViewModel(ITreeService treeService, ILocalizationService localization, IClipboardService clipboard)
    {
        _treeService = treeService;
        _localization = localization;
        _clipboardController = new TreeNodeClipboardController(this, clipboard);

        RemoveNodeCommand = new RelayCommand(parameter => RemoveNode(parameter as TreeNodeViewModel));
        AddSubfolderCommand = new AsyncRelayCommand(async owner => await AddSubfolderAsync(owner));
        AddRootCommand = new AsyncRelayCommand(async owner => await AddRootAsync(owner));
    }

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();

    public ICommand RemoveNodeCommand { get; }

    public ICommand CopyCommand => _clipboardController.CopyCommand;

    public ICommand CutCommand => _clipboardController.CutCommand;

    public ICommand PasteCommand => _clipboardController.PasteCommand;

    public ICommand DeleteCommand => _clipboardController.DeleteCommand;

    public ICommand CopyNameCommand => _clipboardController.CopyNameCommand;

    public ICommand PasteNameCommand => _clipboardController.PasteNameCommand;

    public ICommand AddSubfolderCommand { get; }

    public ICommand AddRootCommand { get; }

    public void OnTreeChanged()
    {
        // The new-tree editor has no preview panel; nothing to refresh.
    }

    public TreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    /// <summary>The node the right-click context menu is attached to.</summary>
    public TreeNodeViewModel? ContextMenuNode
    {
        get => _contextMenuNode;
        set => SetProperty(ref _contextMenuNode, value);
    }

    public bool IsNodeContextMenuOpen
    {
        get => _isNodeContextMenuOpen;
        set => SetProperty(ref _isNodeContextMenuOpen, value);
    }

    public void OpenNodeContextMenu(TreeNodeViewModel node)
    {
        ContextMenuNode = node;
        IsNodeContextMenuOpen = true;
    }

    public bool Saved { get; private set; }

    public string TreeName
    {
        get => _treeName;
        set => SetProperty(ref _treeName, value);
    }

    public string WindowTitle => _localization["NewTree"];

    public string ParentFolderLabel => _localization["ParentFolder"];

    public string AddSubfolderLabel => _localization["AddSubfolder"];

    public string AddRootLabel => _localization["AddRoot"];

    public string AddLabel => _localization["Add"];

    public string RemoveLabel => _localization["Remove"];

    public string RenameLabel => _localization["Rename"];

    public string CopyLabel => _localization["Copy"];

    public string CutLabel => _localization["Cut"];

    public string PasteLabel => _localization["Paste"];

    public string DeleteLabel => _localization["Delete"];

    public string CopyNameLabel => _localization["CopyName"];

    public string PasteNameLabel => _localization["PasteName"];

    public string SaveText => _localization["Save"];

    public string CancelText => _localization["Cancel"];

    public string EnterNamePromptLabel => _localization["EnterNamePrompt"];

    public string ConfirmLabel => _localization["Confirm"];

    public FlowDirection Direction => _localization.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public void AddRoot(string? name)
    {
        RootNodes.Add(new TreeNodeViewModel(this) { Name = NormalizeName(name) });
    }

    public void AddChild(TreeNodeViewModel node, string? name)
    {
        node.Children.Add(new TreeNodeViewModel(this) { Parent = node, Name = NormalizeName(name) });
    }

    public async Task AddSubfolderAsync(object? owner)
    {
        if (owner is not Window window)
        {
            return;
        }

        if (SelectedNode is null)
        {
            return;
        }

        var name = await PromptForNameAsync(window);
        if (name is null)
        {
            return;
        }

        AddChild(SelectedNode, name);
    }

    private async Task AddRootAsync(object? owner)
    {
        if (owner is not Window window)
        {
            return;
        }

        var name = await PromptForNameAsync(window);
        if (name is null)
        {
            return;
        }

        AddRoot(name);
    }

    private async Task<string?> PromptForNameAsync(Window window)
    {
        var dialog = new NameInputWindow(
            WindowTitle,
            EnterNamePromptLabel,
            ConfirmLabel,
            CancelText);

        var confirmed = await dialog.ShowDialog<bool>(window);
        return confirmed ? dialog.Result : null;
    }

    public async Task<TreeModel> SaveAsync(string? targetPath)
    {
        var tree = new TreeModel
        {
            Name = string.IsNullOrWhiteSpace(TreeName) ? "Untitled" : TreeName.Trim(),
            Nodes = ToModels(RootNodes),
        };

        // Working copy inside the program (appears in the Saved Trees list)…
        await _treeService.SaveTreeAsync(tree);
        // …and the copy at the location the user chose.
        await _treeService.SaveTreeToFileAsync(tree, targetPath);

        Saved = true;
        return tree;
    }

    private void RemoveNode(TreeNodeViewModel? node)
    {
        if (node is null)
        {
            return;
        }

        if (node.Parent is null)
        {
            RootNodes.Remove(node);
        }
        else
        {
            node.Parent.Children.Remove(node);
        }
    }

    private string NormalizeName(string? name)
        => string.IsNullOrWhiteSpace(name) ? _localization["NewFolder"] : name.Trim();

    private static List<TreeNodeModel> ToModels(IEnumerable<TreeNodeViewModel> nodes)
        => nodes.Select(node => new TreeNodeModel
        {
            Name = node.Name,
            CreatedAt = node.CreatedAt,
            Children = ToModels(node.Children),
        }).ToList();
}
