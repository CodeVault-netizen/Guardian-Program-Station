using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Helpers;
using Guardian.ProgramStation.UI.Services;
using Guardian.ProgramStation.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class TreeManagerViewModel : ObservableObject, ITreeNodeHost
{
    private readonly ITreeService _treeService;
    private readonly CreateTreeUseCase _createTree;
    private readonly ImportTreeUseCase _importTree;
    private readonly ILocalizationService _localization;
    private readonly ISettingsService _settingsService;
    private readonly IClipboardService _clipboard;
    private readonly TreeNodeClipboardController _clipboardController;
    private readonly Func<Task>? _onProgramsChanged;
    private TreeModel? _current;
    private string _treeName = string.Empty;
    private TreeModel? _selectedTree;
    private TreeNodeViewModel? _selectedNode;
    private string _statusMessage = string.Empty;
    private string _savePath = string.Empty;
    private bool _isDirty;
    private bool _isSortMenuOpen;
    private TreeNodeViewModel? _contextMenuNode;
    private bool _isNodeContextMenuOpen;
    private string? _lastSaveFolder;
    private bool _settingsLoaded;
    private string _asciiPreview = string.Empty;
    private bool _importFoldersOnly;
    private bool _isAsciiVisible = true;
    private TreeNodeViewModel? _popupNode;
    private bool _isNodePopupOpen;
    private string _newNodeName = string.Empty;
    private bool _newNodeIsFolder = true;

    public TreeManagerViewModel(IServiceProvider services, ILocalizationService localization, Func<Task>? onProgramsChanged = null)
    {
        _treeService = services.GetRequiredService<ITreeService>();
        _createTree = services.GetRequiredService<CreateTreeUseCase>();
        _importTree = services.GetRequiredService<ImportTreeUseCase>();
        _localization = localization;
        _settingsService = services.GetRequiredService<ISettingsService>();
        _clipboard = services.GetRequiredService<IClipboardService>();
        _clipboardController = new TreeNodeClipboardController(this, _clipboard);
        _onProgramsChanged = onProgramsChanged;

        OpenTreeCommand = new AsyncRelayCommand(async owner => await OpenTreeAsync(owner));
        NewTreeCommand = new AsyncRelayCommand(async owner => await NewTreeAsync(owner));
        SaveTreeCommand = new AsyncRelayCommand(async owner => await SaveTreeWithDialogAsync(owner));
        SaveTreeAsCommand = new AsyncRelayCommand(async owner => await SaveTreeAsAsync(owner));
        ExportTextCommand = new AsyncRelayCommand(async owner => await ExportTreeAsTextAsync(owner));
        ExportImageCommand = new AsyncRelayCommand(async owner => await ExportTreeAsImageAsync(owner));
        CreateOnDiskCommand = new AsyncRelayCommand(async owner => await CreateOnDiskAsync(owner));
        ImportFromPathCommand = new AsyncRelayCommand(async owner => await ImportFromPathAsync(owner));
        DeleteCurrentTreeCommand = new AsyncRelayCommand(async owner => await DeleteCurrentTreeAsync(owner));
        CloseTreeCommand = new RelayCommand(_ => CloseCurrentTree());
        RemoveNodeCommand = new RelayCommand(parameter => RemoveNode(parameter as TreeNodeViewModel));
        AddSubfolderCommand = new AsyncRelayCommand(async owner => await AddSubfolderAsync(owner));
        AddRootCommand = new AsyncRelayCommand(async owner => await AddRootAsync(owner));
        AddNodeCommand = new RelayCommand(_ => AddNodeFromPopup());
        CancelNodeEditCommand = new RelayCommand(_ => CloseNodePopup());
        DeleteEditNodeCommand = new RelayCommand(_ => DeleteEditNode());
        SetNodeTypeCommand = new RelayCommand(parameter =>
            NewNodeIsFolder = !string.Equals(parameter?.ToString(), "file", StringComparison.OrdinalIgnoreCase));
        SortCommand = new RelayCommand(parameter =>
        {
            if (Enum.TryParse<TreeNodeSortOption>(parameter?.ToString(), ignoreCase: true, out var option))
            {
                SortTree(option);
            }

            IsSortMenuOpen = false;
        });
    }

    public ObservableCollection<TreeModel> SavedTrees { get; } = new();

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();

    public ITreeService TreeService => _treeService;

    public ILocalizationService Localization => _localization;

    public ICommand OpenTreeCommand { get; }

    public ICommand NewTreeCommand { get; }

    public ICommand SaveTreeCommand { get; }

    public ICommand SaveTreeAsCommand { get; }

    public ICommand ExportTextCommand { get; }

    public ICommand ExportImageCommand { get; }

    public ICommand SortCommand { get; }

    public ICommand CreateOnDiskCommand { get; }

    public ICommand ImportFromPathCommand { get; }

    public ICommand DeleteCurrentTreeCommand { get; }

    public ICommand CloseTreeCommand { get; }

    public ICommand RemoveNodeCommand { get; }

    public ICommand CopyCommand => _clipboardController.CopyCommand;

    public ICommand CutCommand => _clipboardController.CutCommand;

    public ICommand PasteCommand => _clipboardController.PasteCommand;

    public ICommand DeleteCommand => _clipboardController.DeleteCommand;

    public ICommand CopyNameCommand => _clipboardController.CopyNameCommand;

    public ICommand PasteNameCommand => _clipboardController.PasteNameCommand;

    public ICommand AddSubfolderCommand { get; }

    public ICommand AddRootCommand { get; }

    public ICommand AddNodeCommand { get; }

    public ICommand CancelNodeEditCommand { get; }

    public ICommand DeleteEditNodeCommand { get; }

    public ICommand SetNodeTypeCommand { get; }

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

    /// <summary>The node the inline popup is attached to.</summary>
    public TreeNodeViewModel? PopupNode
    {
        get => _popupNode;
        set => SetProperty(ref _popupNode, value);
    }

    public bool IsNodePopupOpen
    {
        get => _isNodePopupOpen;
        set => SetProperty(ref _isNodePopupOpen, value);
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

    public string NewNodeName
    {
        get => _newNodeName;
        set => SetProperty(ref _newNodeName, value);
    }

    public bool NewNodeIsFolder
    {
        get => _newNodeIsFolder;
        set
        {
            if (SetProperty(ref _newNodeIsFolder, value))
            {
                OnPropertyChanged(nameof(NewNodeIsFile));
            }
        }
    }

    public bool NewNodeIsFile => !_newNodeIsFolder;

    public string TreeName
    {
        get => _treeName;
        set
        {
            if (SetProperty(ref _treeName, value))
            {
                _isDirty = true;
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>The location the user last chose for the current tree; shown permanently in the editor header.</summary>
    public string SavePath
    {
        get => _savePath;
        private set
        {
            if (SetProperty(ref _savePath, value))
            {
                OnPropertyChanged(nameof(HasSavePath));
            }
        }
    }

    public bool HasSavePath => !string.IsNullOrWhiteSpace(SavePath);

    /// <summary>ASCII tree representation, regenerated on every tree change.</summary>
    public string AsciiPreview
    {
        get => _asciiPreview;
        private set => SetProperty(ref _asciiPreview, value);
    }

    public void OnTreeChanged()
    {
        _isDirty = true;
        RegenerateAsciiPreview();
    }

    /// <summary>Sorts the whole tree — every level, recursively — by the given option.</summary>
    public void SortTree(TreeNodeSortOption option)
    {
        if (RootNodes.Count == 0)
        {
            return;
        }

        SortNodes(RootNodes, option);
        OnTreeChanged();
        _isDirty = true;
        StatusMessage = _localization["TreeSorted"];
    }

    private static void SortNodes(IList<TreeNodeViewModel> nodes, TreeNodeSortOption option)
    {
        // Sort every level below first, so the whole tree is sorted recursively.
        foreach (var node in nodes)
        {
            SortNodes(node.Children, option);
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

    /// <summary>True when the current tree was edited after its last save; drives the save prompt on close.</summary>
    public bool HasUnsavedChanges => _isDirty && (RootNodes.Count > 0 || _current is not null);

    /// <summary>Drives the Sort menu popup (same mechanism as the node-add popup).</summary>
    public bool IsSortMenuOpen
    {
        get => _isSortMenuOpen;
        set => SetProperty(ref _isSortMenuOpen, value);
    }

    public string NewLabel => _localization["New"];

    public string OpenLabel => _localization["Open"];

    public string SaveLabel => _localization["Save"];

    public string SaveAsLabel => _localization["SaveAs"];

    public string ExportTextLabel => _localization["ExportText"];

    public string ExportImageLabel => _localization["ExportImage"];

    public string SortLabel => _localization["Sort"];

    public string SortNameAscLabel => _localization["SortNameAscending"];

    public string SortNameDescLabel => _localization["SortNameDescending"];

    public string SortOldestFirstLabel => _localization["SortOldestFirst"];

    public string SortNewestFirstLabel => _localization["SortNewestFirst"];

    public string CreateOnDiskLabel => _localization["CreateOnDisk"];

    public string ImportFromPathLabel => _localization["ImportFromPath"];

    public string ImportFoldersOnlyLabel => _localization["ImportFoldersOnly"];

    public string ImportFoldersOnlyHint => _localization["ImportFoldersOnlyHint"];

    public bool ImportFoldersOnly
    {
        get => _importFoldersOnly;
        set => SetProperty(ref _importFoldersOnly, value);
    }

    public string AddRootLabel => _localization["AddRoot"];

    public string AddLabel => _localization["Add"];

    public string RemoveLabel => _localization["Remove"];

    public string RenameLabel => _localization["Rename"];

    public string AddNodeLabel => _localization["AddNode"];

    public string FileLabel => _localization["File"];

    public string FolderLabel => _localization["Folder"];

    public string DeleteNodeLabel => _localization["DeleteNode"];

    public string CopyLabel => _localization["Copy"];

    public string CutLabel => _localization["Cut"];

    public string PasteLabel => _localization["Paste"];

    public string DeleteLabel => _localization["Delete"];

    public string CopyNameLabel => _localization["CopyName"];

    public string PasteNameLabel => _localization["PasteName"];

    public string NewNodePromptLabel => _localization["NewNodePrompt"];

    public string TreeNameLabel => _localization["TreeName"];

    public string EditingLabel => _localization["Editing"];

    public string CloseLabel => _localization["Close"];

    public string SavedTreesLabel => _localization["SavedTrees"];

    public string SavePathLabel => _localization["SavePath"];

    public string TreePreviewLabel => _localization["TreePreview"];

    public string AsciiPreviewLabel => _localization["AsciiPreview"];

    /// <summary>True while the Ascii panel shares the space; false collapses it to zero width.</summary>
    public bool IsAsciiVisible
    {
        get => _isAsciiVisible;
        set
        {
            if (SetProperty(ref _isAsciiVisible, value))
            {
                OnPropertyChanged(nameof(PreviewColumns));
            }
        }
    }

    /// <summary>Dynamic column split: 1.5*/1* when Ascii is visible, * /0 when hidden.</summary>
    public string PreviewColumns => IsAsciiVisible ? "1.5*,1*" : "*,0";

    public string RestorePanelLabel => _localization["RestorePanel"];

    public string MinimizeLabel => _localization["Minimize"];

    public string MaximizeLabel => _localization["Maximize"];

    public string TreeStructureLabel => _localization["TreeStructure"];

    public string EnterNamePromptLabel => _localization["EnterNamePrompt"];

    public string ConfirmLabel => _localization["Confirm"];

    public string CancelLabel => _localization["Cancel"];

    public string DeleteTreeLabel => _localization["DeleteTree"];

    public string ConfirmDeleteTreeLabel => _localization["ConfirmDeleteTree"];

    public async Task LoadAsync()
    {
        if (!_settingsLoaded)
        {
            try
            {
                _lastSaveFolder = (await _settingsService.LoadAsync()).LastSaveFolder;
            }
            catch
            {
                // Ignore: the save dialog falls back to its default folder.
            }

            _settingsLoaded = true;
        }

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
        OnTreeChanged();
    }

    public void AddChild(TreeNodeViewModel node, string? name)
    {
        node.Children.Add(new TreeNodeViewModel(this) { Parent = node, Name = NormalizeName(name) });
        OnTreeChanged();
    }

    public void OpenNodePopup(TreeNodeViewModel node)
    {
        PopupNode = node;
        NewNodeName = string.Empty;
        NewNodeIsFolder = true;
        IsNodePopupOpen = true;
    }

    private void CloseNodePopup() => IsNodePopupOpen = false;

    private void AddNodeFromPopup()
    {
        if (PopupNode is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewNodeName)
            ? (NewNodeIsFolder ? _localization["NewFolder"] : _localization["NewFile"])
            : NewNodeName.Trim();

        var child = new TreeNodeViewModel(this)
        {
            Parent = PopupNode,
            Name = name,
            NodeType = NewNodeIsFolder ? "folder" : "file",
        };

        PopupNode.Children.Add(child);
        CloseNodePopup();
    }

    private void DeleteEditNode()
    {
        if (PopupNode is null)
        {
            return;
        }

        RemoveNode(PopupNode);
        CloseNodePopup();
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
            _isDirty = false;
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

        var dialog = new TreeEditorWindow(new TreeEditorViewModel(_treeService, _localization, _clipboard), _lastSaveFolder);
        var result = await dialog.ShowDialog<TreeEditorResult?>(window);

        if (result is not null)
        {
            await LoadAsync();
            OpenSavedTree(result.Tree);
            SavePath = result.SavePath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(result.SavePath))
            {
                await RememberSaveFolderAsync(result.SavePath);
            }

            StatusMessage = _localization["TreeCreated"];
        }
    }

    private async Task OpenTreeAsync(object? owner)
    {
        if (owner is not Window window)
        {
            return;
        }

        var startFolder = await window.StorageProvider.TryGetFolderFromPathAsync(PathHelper.GetTreesDirectory());
        var file = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _localization["Open"],
            AllowMultiple = false,
            SuggestedStartLocation = startFolder,
            FileTypeFilter = new[] { new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } } },
        });

        if (file.Count == 0)
        {
            return;
        }

        var filePath = file[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        TreeModel? loaded;
        try
        {
            loaded = await _treeService.LoadTreeFromFileAsync(filePath);
        }
        catch (Exception)
        {
            StatusMessage = _localization["TreeOpenError"];
            return;
        }

        if (loaded is null)
        {
            StatusMessage = _localization["TreeOpenError"];
            return;
        }

        _current = loaded;
        SelectedTree = loaded;
        TreeName = _current.Name;
        SavePath = string.Empty;
        RootNodes.Clear();
        foreach (var node in _current.Nodes)
        {
            RootNodes.Add(CreateNode(node, null));
        }

        OnTreeChanged();
        _isDirty = false;
        StatusMessage = _localization["TreeOpened"] + " · " + filePath;
    }

    /// <summary>Opens a tree picked from the saved-trees list into the editor.</summary>
    public void OpenSavedTree(TreeModel tree)
    {
        if (tree is null || _current?.Id == tree.Id)
        {
            return;
        }

        _current = tree;
        SelectedTree = tree;
        TreeName = tree.Name;
        SavePath = string.Empty;
        RootNodes.Clear();
        foreach (var node in tree.Nodes)
        {
            RootNodes.Add(CreateNode(node, null));
        }

        OnTreeChanged();
        _isDirty = false;
        StatusMessage = _localization["TreeOpened"] + " · " + tree.Name;
    }

    private void CloseCurrentTree()
    {
        _current = null;
        SelectedTree = null;
        SelectedNode = null;
        PopupNode = null;
        IsNodePopupOpen = false;
        TreeName = string.Empty;
        SavePath = string.Empty;
        RootNodes.Clear();
        AsciiPreview = string.Empty;
        _isDirty = false;
        StatusMessage = _localization["TreeClosed"];
    }

    private async Task DeleteCurrentTreeAsync(object? owner)
    {
        if (_current is null)
        {
            StatusMessage = _localization["SelectTreeFirst"];
            return;
        }

        if (owner is not Window window)
        {
            return;
        }

        var dialog = new ConfirmWindow(
            DeleteTreeLabel,
            ConfirmDeleteTreeLabel,
            ConfirmLabel,
            CancelLabel);

        var confirmed = await dialog.ShowDialog<bool>(window);
        if (!confirmed)
        {
            return;
        }

        await DeleteTreeAsync(_current);
        StatusMessage = _localization["TreeDeleted"];
    }

    private async Task SaveTreeAsync()
    {
        try
        {
            if (_current is null)
            {
                _current = new TreeModel();
            }

            _current.Name = string.IsNullOrWhiteSpace(TreeName) ? "Untitled" : TreeName;
            _current.Nodes = ToModels(RootNodes);

            await _treeService.SaveTreeAsync(_current);
            await LoadAsync();
            StatusMessage = _localization["TreeSaved"] + " · " + GetTreeFilePath(_current.Id);
        }
        catch (Exception)
        {
            StatusMessage = _localization["TreeSaveError"];
        }
    }

    /// <summary>Resolves the window to anchor dialogs: the CommandParameter first, then the app's main window.</summary>
    private static Window? ResolveWindow(object? owner)
    {
        if (owner is Window window)
        {
            return window;
        }

        return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop
            ? desktop.MainWindow
            : null;
    }

    /// <summary>True when the current tree is clean or was saved; false when the user cancelled the save dialog.</summary>
    public async Task<bool> TrySaveCurrentTreeAsync(object? owner)
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        return await SaveTreeWithDialogAsync(owner);
    }

    private async Task<bool> SaveTreeWithDialogAsync(object? owner)
    {
        try
        {
            if (ResolveWindow(owner) is not { } window)
            {
                StatusMessage = _localization["TreeSaveError"];
                return false;
            }

            if (_current is null)
            {
                _current = new TreeModel();
            }

            _current.Name = string.IsNullOrWhiteSpace(TreeName) ? "Untitled" : TreeName;
            _current.Nodes = ToModels(RootNodes);

            var startFolder = !string.IsNullOrWhiteSpace(_lastSaveFolder)
                ? await window.StorageProvider.TryGetFolderFromPathAsync(_lastSaveFolder)
                : null;

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = _localization["Save"],
                SuggestedFileName = PathHelper.SanitizeFileName(_current.Name) + ".json",
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON file") },
                SuggestedStartLocation = startFolder,
            });

            if (file is null)
            {
                // Cancel: nothing is written.
                return false;
            }

            var targetPath = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return false;
            }

            // Save writes ONLY to the location the user chose; nothing is written
            // to the app's internal folder (the user determines the save path).
            await _treeService.SaveTreeToFileAsync(_current, targetPath);
            SavePath = targetPath;
            _isDirty = false;
            await RememberSaveFolderAsync(targetPath);
            StatusMessage = _localization["TreeSaved"] + " · " + targetPath;
            return true;
        }
        catch (Exception)
        {
            StatusMessage = _localization["TreeSaveError"];
            return false;
        }
    }

    private async Task SaveTreeAsAsync(object? owner)
    {
        try
        {
            _current = new TreeModel();
            await SaveTreeAsync();

            if (owner is not Window window || _current is null)
            {
                return;
            }

            var startFolder = !string.IsNullOrWhiteSpace(_lastSaveFolder)
                ? await window.StorageProvider.TryGetFolderFromPathAsync(_lastSaveFolder)
                : null;

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = _localization["SaveAs"],
                SuggestedFileName = PathHelper.SanitizeFileName(_current.Name) + ".json",
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON file") },
                SuggestedStartLocation = startFolder,
            });

            if (file is null)
            {
                return;
            }

            var targetPath = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            File.Copy(GetTreeFilePath(_current.Id), targetPath, overwrite: true);
            SavePath = targetPath;
            _isDirty = false;
            await RememberSaveFolderAsync(targetPath);
            StatusMessage = _localization["TreeSavedAs"] + " " + targetPath;
        }
        catch (Exception)
        {
            StatusMessage = _localization["TreeSaveError"];
        }
    }

    private async Task ExportTreeAsTextAsync(object? owner)
    {
        try
        {
            if (ResolveWindow(owner) is not { } window)
            {
                return;
            }

            if (RootNodes.Count == 0)
            {
                StatusMessage = _localization["TreeEmpty"];
                return;
            }

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = _localization["ExportText"],
                SuggestedFileName = PathHelper.SanitizeFileName(string.IsNullOrWhiteSpace(TreeName) ? "tree" : TreeName) + ".txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { new FilePickerFileType("Text file") { Patterns = new[] { "*.txt" } } },
                SuggestedStartLocation = await ResolveSaveStartFolderAsync(window),
            });

            if (file is null)
            {
                return;
            }

            var targetPath = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            var header = string.IsNullOrWhiteSpace(TreeName) ? string.Empty : TreeName.Trim() + Environment.NewLine + Environment.NewLine;
            await File.WriteAllTextAsync(targetPath, header + AsciiPreview);
            await RememberSaveFolderAsync(targetPath);
            StatusMessage = _localization["TreeTextExported"] + " · " + targetPath;
        }
        catch (Exception)
        {
            StatusMessage = _localization["TreeExportError"];
        }
    }

    private async Task ExportTreeAsImageAsync(object? owner)
    {
        try
        {
            if (ResolveWindow(owner) is not { } window)
            {
                return;
            }

            if (RootNodes.Count == 0)
            {
                StatusMessage = _localization["TreeEmpty"];
                return;
            }

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = _localization["ExportImage"],
                SuggestedFileName = PathHelper.SanitizeFileName(string.IsNullOrWhiteSpace(TreeName) ? "tree" : TreeName) + ".png",
                DefaultExtension = "png",
                FileTypeChoices = new[] { new FilePickerFileType("PNG image") { Patterns = new[] { "*.png" } } },
                SuggestedStartLocation = await ResolveSaveStartFolderAsync(window),
            });

            if (file is null)
            {
                return;
            }

            var targetPath = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            var header = string.IsNullOrWhiteSpace(TreeName) ? string.Empty : TreeName.Trim() + Environment.NewLine + Environment.NewLine;
            TreeImageExporter.SaveTextAsPng(header + AsciiPreview, targetPath);
            await RememberSaveFolderAsync(targetPath);
            StatusMessage = _localization["TreeImageExported"] + " · " + targetPath;
        }
        catch (Exception)
        {
            StatusMessage = _localization["TreeExportError"];
        }
    }

    private static string GetTreeFilePath(string treeId)
        => Path.Combine(PathHelper.GetTreesDirectory(), $"{treeId}.json");

    /// <summary>Resolves the folder the save/export dialog should open on: the last folder the user used.</summary>
    private async Task<IStorageFolder?> ResolveSaveStartFolderAsync(Window window)
    {
        return !string.IsNullOrWhiteSpace(_lastSaveFolder)
            ? await window.StorageProvider.TryGetFolderFromPathAsync(_lastSaveFolder)
            : null;
    }

    /// <summary>Remembers the folder of a successful save so the next save dialog opens there.</summary>
    private async Task RememberSaveFolderAsync(string targetPath)
    {
        var folder = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(folder) || string.Equals(folder, _lastSaveFolder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastSaveFolder = folder;

        try
        {
            var settings = await _settingsService.LoadAsync();
            settings.LastSaveFolder = folder;
            await _settingsService.SaveAsync(settings);
        }
        catch
        {
            // Non-critical: the next save dialog just falls back to its default folder.
        }
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

        var result = await Task.Run(() => _importTree.ExecuteAsync(rootPath, ImportFoldersOnly));

        var tree = result.Tree;
        _current = tree;
        TreeName = tree.Name;
        RootNodes.Clear();
        foreach (var node in tree.Nodes)
        {
            RootNodes.Add(CreateNode(node, null));
        }

        OnTreeChanged();
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

        OnTreeChanged();
    }

    private TreeNodeViewModel CreateNode(TreeNodeModel model, TreeNodeViewModel? parent)
    {
        var viewModel = new TreeNodeViewModel(this)
        {
            Parent = parent,
            Name = model.Name,
            NodeType = model.NodeType,
            CreatedAt = model.CreatedAt,
        };

        foreach (var child in model.Children)
        {
            viewModel.Children.Add(CreateNode(child, viewModel));
        }

        return viewModel;
    }

    private void RegenerateAsciiPreview()
    {
        var builder = new System.Text.StringBuilder();
        AppendNodes(builder, RootNodes, "");
        AsciiPreview = builder.ToString().TrimEnd();
    }

    private static void AppendNodes(System.Text.StringBuilder builder, IEnumerable<TreeNodeViewModel> nodes, string indent)
    {
        var list = nodes.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var isLast = i == list.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            builder.Append(indent).Append(connector).AppendLine(list[i].Name);

            var childIndent = indent + (isLast ? "    " : "│   ");
            AppendNodes(builder, list[i].Children, childIndent);
        }
    }

    private static List<TreeNodeModel> ToModels(IEnumerable<TreeNodeViewModel> nodes)
        => nodes.Select(node => new TreeNodeModel
        {
            Name = node.Name,
            NodeType = node.NodeType,
            CreatedAt = node.CreatedAt,
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
        OnPropertyChanged(nameof(ImportFoldersOnlyLabel));
        OnPropertyChanged(nameof(ImportFoldersOnlyHint));
        OnPropertyChanged(nameof(AddRootLabel));
        OnPropertyChanged(nameof(AddLabel));
        OnPropertyChanged(nameof(RemoveLabel));
        OnPropertyChanged(nameof(RenameLabel));
        OnPropertyChanged(nameof(AddNodeLabel));
        OnPropertyChanged(nameof(FileLabel));
        OnPropertyChanged(nameof(FolderLabel));
        OnPropertyChanged(nameof(DeleteNodeLabel));
        OnPropertyChanged(nameof(CopyLabel));
        OnPropertyChanged(nameof(CutLabel));
        OnPropertyChanged(nameof(PasteLabel));
        OnPropertyChanged(nameof(DeleteLabel));
        OnPropertyChanged(nameof(CopyNameLabel));
        OnPropertyChanged(nameof(PasteNameLabel));
        OnPropertyChanged(nameof(NewNodePromptLabel));
        OnPropertyChanged(nameof(TreeNameLabel));
        OnPropertyChanged(nameof(EditingLabel));
        OnPropertyChanged(nameof(CloseLabel));
        OnPropertyChanged(nameof(SavedTreesLabel));
        OnPropertyChanged(nameof(TreePreviewLabel));
        OnPropertyChanged(nameof(AsciiPreviewLabel));
        OnPropertyChanged(nameof(RestorePanelLabel));
        OnPropertyChanged(nameof(MinimizeLabel));
        OnPropertyChanged(nameof(MaximizeLabel));
        OnPropertyChanged(nameof(TreeStructureLabel));
        OnPropertyChanged(nameof(EnterNamePromptLabel));
        OnPropertyChanged(nameof(ConfirmLabel));
        OnPropertyChanged(nameof(CancelLabel));
        OnPropertyChanged(nameof(DeleteTreeLabel));
        OnPropertyChanged(nameof(ConfirmDeleteTreeLabel));
        OnPropertyChanged(nameof(ExportTextLabel));
        OnPropertyChanged(nameof(ExportImageLabel));
        OnPropertyChanged(nameof(SortLabel));
        OnPropertyChanged(nameof(SortNameAscLabel));
        OnPropertyChanged(nameof(SortNameDescLabel));
        OnPropertyChanged(nameof(SortOldestFirstLabel));
        OnPropertyChanged(nameof(SortNewestFirstLabel));
    }
}
