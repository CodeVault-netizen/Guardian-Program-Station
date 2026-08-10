using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class TreeManagerViewModel : ObservableObject, ITreeNodeHost
{
    private readonly ITreeService _treeService;
    private readonly CreateTreeUseCase _createTree;
    private readonly ImportTreeUseCase _importTree;
    private readonly ILocalizationService _localization;
    private readonly Func<Task>? _onProgramsChanged;
    private TreeModel? _current;
    private string _treeName = string.Empty;
    private TreeModel? _selectedTree;
    private TreeNodeViewModel? _selectedNode;
    private string _statusMessage = string.Empty;

    public TreeManagerViewModel(IServiceProvider services, ILocalizationService localization, Func<Task>? onProgramsChanged = null)
    {
        _treeService = services.GetRequiredService<ITreeService>();
        _createTree = services.GetRequiredService<CreateTreeUseCase>();
        _importTree = services.GetRequiredService<ImportTreeUseCase>();
        _localization = localization;
        _onProgramsChanged = onProgramsChanged;

        OpenTreeCommand = new AsyncRelayCommand(async _ => await OpenTreeAsync());
        NewTreeCommand = new AsyncRelayCommand(async owner => await NewTreeAsync(owner));
        SaveTreeCommand = new AsyncRelayCommand(async _ => await SaveTreeAsync());
        SaveTreeAsCommand = new AsyncRelayCommand(async _ => await SaveTreeAsAsync());
        CreateOnDiskCommand = new AsyncRelayCommand(async owner => await CreateOnDiskAsync(owner));
        ImportFromPathCommand = new AsyncRelayCommand(async owner => await ImportFromPathAsync(owner));
        RemoveNodeCommand = new RelayCommand(parameter => RemoveNode(parameter as TreeNodeViewModel));
        AddSubfolderCommand = new AsyncRelayCommand(async owner => await AddSubfolderAsync(owner));
        AddRootCommand = new AsyncRelayCommand(async owner => await AddRootAsync(owner));
    }

    public ObservableCollection<TreeModel> SavedTrees { get; } = new();

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();

    public ITreeService TreeService => _treeService;

    public ILocalizationService Localization => _localization;

    public ICommand OpenTreeCommand { get; }

    public ICommand NewTreeCommand { get; }

    public ICommand SaveTreeCommand { get; }

    public ICommand SaveTreeAsCommand { get; }

    public ICommand CreateOnDiskCommand { get; }

    public ICommand ImportFromPathCommand { get; }

    public ICommand RemoveNodeCommand { get; }

    public ICommand AddSubfolderCommand { get; }

    public ICommand AddRootCommand { get; }

    public TreeModel? SelectedTree
    {
        get => _selectedTree;
        set => SetProperty(ref _selectedTree, value);
    }

    public TreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    public string TreeName
    {
        get => _treeName;
        set => SetProperty(ref _treeName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string NewLabel => _localization["New"];

    public string OpenLabel => _localization["Open"];

    public string SaveLabel => _localization["Save"];

    public string SaveAsLabel => _localization["SaveAs"];

    public string CreateOnDiskLabel => _localization["CreateOnDisk"];

    public string ImportFromPathLabel => _localization["ImportFromPath"];

    public string AddRootLabel => _localization["AddRoot"];

    public string AddLabel => _localization["Add"];

    public string RemoveLabel => _localization["Remove"];

    public string TreeNameLabel => _localization["TreeName"];

    public string SavedTreesLabel => _localization["SavedTrees"];

    public string TreeStructureLabel => _localization["TreeStructure"];

    public string EnterNamePromptLabel => _localization["EnterNamePrompt"];

    public string ConfirmLabel => _localization["Confirm"];

    public string CancelLabel => _localization["Cancel"];

    public string DeleteTreeLabel => _localization["DeleteTree"];

    public string ConfirmDeleteTreeLabel => _localization["ConfirmDeleteTree"];

    public async Task LoadAsync()
    {
        var trees = await _treeService.LoadTreesAsync();
        SavedTrees.Clear();
        foreach (var tree in trees)
        {
            SavedTrees.Add(tree);
        }
    }

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
            StatusMessage = _localization["SelectNodeFirst"];
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
            TreeStructureLabel,
            EnterNamePromptLabel,
            ConfirmLabel,
            CancelLabel);

        var confirmed = await dialog.ShowDialog<bool>(window);
        return confirmed ? dialog.Result : null;
    }

    public async Task DeleteTreeAsync(TreeModel tree)
    {
        if (tree is null)
        {
            return;
        }

        await _treeService.DeleteTreeAsync(tree.Id);
        SavedTrees.Remove(tree);

        if (_current?.Id == tree.Id)
        {
            _current = null;
            TreeName = string.Empty;
            RootNodes.Clear();
        }

        StatusMessage = _localization["TreeDeleted"];
    }

    private string NormalizeName(string? name)
        => string.IsNullOrWhiteSpace(name) ? _localization["NewFolder"] : name.Trim();

    private async Task NewTreeAsync(object? owner)
    {
        if (owner is not Avalonia.Controls.Window window)
        {
            return;
        }

        var dialog = new TreeEditorWindow(new TreeEditorViewModel(_treeService, _localization));
        var saved = await dialog.ShowDialog<bool>(window);

        if (saved)
        {
            await LoadAsync();
            StatusMessage = _localization["TreeCreated"];
        }
    }

    private async Task OpenTreeAsync()
    {
        if (_selectedTree is null)
        {
            StatusMessage = _localization["SelectTreeFirst"];
            return;
        }

        _current = await _treeService.LoadTreeAsync(_selectedTree.Id);
        if (_current is null)
        {
            return;
        }

        TreeName = _current.Name;
        RootNodes.Clear();
        foreach (var node in _current.Nodes)
        {
            RootNodes.Add(CreateNode(node, null));
        }

        StatusMessage = _localization["TreeOpened"];
    }

    private async Task SaveTreeAsync()
    {
        if (_current is null)
        {
            _current = new TreeModel();
        }

        _current.Name = string.IsNullOrWhiteSpace(TreeName) ? "Untitled" : TreeName;
        _current.Nodes = ToModels(RootNodes);

        await _treeService.SaveTreeAsync(_current);
        await LoadAsync();
        StatusMessage = _localization["TreeSaved"];
    }

    private async Task SaveTreeAsAsync()
    {
        _current = new TreeModel();
        await SaveTreeAsync();
    }

    private async Task CreateOnDiskAsync(object? owner)
    {
        if (_current is null || RootNodes.Count == 0)
        {
            StatusMessage = _localization["TreeEmpty"];
            return;
        }

        if (owner is not Avalonia.Controls.Window window)
        {
            return;
        }

        var folder = await window.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = _localization["ChooseRootFolder"],
            AllowMultiple = false,
        });

        if (folder.Count == 0)
        {
            return;
        }

        var rootPath = folder[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return;
        }

        var model = new TreeModel
        {
            Id = _current.Id,
            Name = _current.Name,
            Nodes = ToModels(RootNodes),
        };

        await _createTree.ExecuteAsync(model, rootPath);
        StatusMessage = _localization["TreeCreated"];
    }

    private async Task ImportFromPathAsync(object? owner)
    {
        if (owner is not Avalonia.Controls.Window window)
        {
            return;
        }

        var folder = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = _localization["ChooseImportFolder"],
            AllowMultiple = false,
        });

        if (folder.Count == 0)
        {
            return;
        }

        var rootPath = folder[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return;
        }

        var result = await Task.Run(() => _importTree.ExecuteAsync(rootPath));

        var tree = result.Tree;
        _current = tree;
        TreeName = tree.Name;
        RootNodes.Clear();
        foreach (var node in tree.Nodes)
        {
            RootNodes.Add(CreateNode(node, null));
        }

        await LoadAsync();

        if (result.AddedProgramsCount > 0)
        {
            if (_onProgramsChanged is not null)
            {
                await _onProgramsChanged();
            }

            StatusMessage = _localization["TreeImported"] + " · " + _localization["ProgramsAdded"] + result.AddedProgramsCount;
        }
        else
        {
            StatusMessage = _localization["TreeImported"];
        }
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

    private TreeNodeViewModel CreateNode(TreeNodeModel model, TreeNodeViewModel? parent)
    {
        var viewModel = new TreeNodeViewModel(this) { Parent = parent, Name = model.Name };

        foreach (var child in model.Children)
        {
            viewModel.Children.Add(CreateNode(child, viewModel));
        }

        return viewModel;
    }

    private static List<TreeNodeModel> ToModels(IEnumerable<TreeNodeViewModel> nodes)
        => nodes.Select(node => new TreeNodeModel
        {
            Name = node.Name,
            Children = ToModels(node.Children),
        }).ToList();

    public void RefreshLocalized()
    {
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(NewLabel));
        OnPropertyChanged(nameof(OpenLabel));
        OnPropertyChanged(nameof(SaveLabel));
        OnPropertyChanged(nameof(SaveAsLabel));
        OnPropertyChanged(nameof(CreateOnDiskLabel));
        OnPropertyChanged(nameof(ImportFromPathLabel));
        OnPropertyChanged(nameof(AddRootLabel));
        OnPropertyChanged(nameof(AddLabel));
        OnPropertyChanged(nameof(RemoveLabel));
        OnPropertyChanged(nameof(TreeNameLabel));
        OnPropertyChanged(nameof(SavedTreesLabel));
        OnPropertyChanged(nameof(TreeStructureLabel));
        OnPropertyChanged(nameof(EnterNamePromptLabel));
        OnPropertyChanged(nameof(ConfirmLabel));
        OnPropertyChanged(nameof(CancelLabel));
        OnPropertyChanged(nameof(DeleteTreeLabel));
        OnPropertyChanged(nameof(ConfirmDeleteTreeLabel));
    }
}
