using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.UI.Views;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class TreeEditorViewModel : ObservableObject, ITreeNodeHost
{
    private readonly ITreeService _treeService;
    private readonly ILocalizationService _localization;
    private string _treeName = string.Empty;
    private TreeNodeViewModel? _selectedNode;

    public TreeEditorViewModel(ITreeService treeService, ILocalizationService localization)
    {
        _treeService = treeService;
        _localization = localization;

        RemoveNodeCommand = new RelayCommand(parameter => RemoveNode(parameter as TreeNodeViewModel));
        AddSubfolderCommand = new AsyncRelayCommand(async owner => await AddSubfolderAsync(owner));
        AddRootCommand = new AsyncRelayCommand(async owner => await AddRootAsync(owner));
    }

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();

    public ICommand RemoveNodeCommand { get; }

    public ICommand AddSubfolderCommand { get; }

    public ICommand AddRootCommand { get; }

    public TreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
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

    public async Task SaveAsync()
    {
        var tree = new TreeModel
        {
            Name = string.IsNullOrWhiteSpace(TreeName) ? "Untitled" : TreeName.Trim(),
            Nodes = ToModels(RootNodes),
        };

        await _treeService.SaveTreeAsync(tree);
        Saved = true;
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
            Children = ToModels(node.Children),
        }).ToList();
}
