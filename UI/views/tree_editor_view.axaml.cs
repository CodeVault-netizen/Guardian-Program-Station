using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Guardian.ProgramStation.Infrastructure.Helpers;
using Guardian.ProgramStation.Infrastructure.Services;
using Guardian.ProgramStation.UI.Services;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class TreeEditorWindow : Window
{
    private readonly string? _startFolder;

    public TreeEditorViewModel ViewModel { get; }

    public TreeEditorWindow()
    {
        InitializeComponent();
        ViewModel = new TreeEditorViewModel(new TreeStorageService(), new LocalizationService(), new SystemClipboardService());
        DataContext = ViewModel;
    }

    public TreeEditorWindow(TreeEditorViewModel viewModel, string? startFolder = null)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _startFolder = startFolder;
        DataContext = viewModel;
    }

    // ---- Keyboard shortcuts (Ctrl+C / Ctrl+X / Ctrl+V / Delete) ----
    // Handled here (not via TreeView KeyBindings) so the shortcuts never steal keys
    // from a focused TextBox (node name / tree name). Same fix as TreeManagerView.

    private void OnTreeEditorKeyDown(object? sender, KeyEventArgs e)
    {
        // A focused TextBox handles these keys itself.
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox)
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.C:
                    ViewModel.CopyCommand.Execute(ViewModel.SelectedNode);
                    e.Handled = true;
                    return;
                case Key.X:
                    ViewModel.CutCommand.Execute(ViewModel.SelectedNode);
                    e.Handled = true;
                    return;
                case Key.V:
                    ViewModel.PasteCommand.Execute(ViewModel.SelectedNode);
                    e.Handled = true;
                    return;
            }
        }
        else if (e.Key == Key.Delete)
        {
            ViewModel.DeleteCommand.Execute(ViewModel.SelectedNode);
            e.Handled = true;
        }
    }

    private async void OnAddChild(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Control { DataContext: TreeNodeViewModel node })
            {
                return;
            }

            ViewModel.SelectedNode = node;
            await ViewModel.AddSubfolderAsync(this);
        }
        catch
        {
            // Ignore prompt failures.
        }
    }

    private void OnNodeContextMenuPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TreeNodeViewModel node })
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        ViewModel.SelectedNode = node;
        ViewModel.OpenNodeContextMenu(node);
        e.Handled = true;
    }

    private void OnNodeContextAction(object? sender, RoutedEventArgs e)
    {
        // The command ran first; close the popup like a native context menu.
        ViewModel.IsNodeContextMenuOpen = false;
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        try
        {
            // The user always chooses the save location; nothing is written on cancel.
            var suggestedName = (string.IsNullOrWhiteSpace(ViewModel.TreeName) ? "Untitled" : ViewModel.TreeName.Trim()) + ".json";

            var startFolder = !string.IsNullOrWhiteSpace(_startFolder)
                ? await StorageProvider.TryGetFolderFromPathAsync(_startFolder)
                : null;

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = ViewModel.SaveText,
                SuggestedFileName = PathHelper.SanitizeFileName(suggestedName),
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON file") },
                SuggestedStartLocation = startFolder,
            });

            if (file is null)
            {
                // Cancel: nothing is written, the dialog stays open.
                return;
            }

            var targetPath = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            var tree = await ViewModel.SaveAsync(targetPath);
            Close(new TreeEditorResult(tree, targetPath));
        }
        catch
        {
            // Keep the dialog open so the user can retry.
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private async Task<string?> PromptForNameAsync()
    {
        var dialog = new NameInputWindow(
            ViewModel.WindowTitle,
            ViewModel.EnterNamePromptLabel,
            ViewModel.ConfirmLabel,
            ViewModel.CancelText);

        var confirmed = await dialog.ShowDialog<bool>(this);
        return confirmed ? dialog.Result : null;
    }
}
