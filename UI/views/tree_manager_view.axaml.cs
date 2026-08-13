using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class TreeManagerView : UserControl
{
    private TreeManagerViewModel? ViewModel => DataContext as TreeManagerViewModel;

    private readonly Dictionary<TreeNodeViewModel, string> _renameOriginals = new();

    public TreeManagerView()
    {
        InitializeComponent();
    }

    public TreeManagerView(TreeManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        EditorTreeView.ContainerPrepared += OnEditorTreeContainerPrepared;

        // TextBox and Button consume PointerPressed (mark it Handled), so a normal
        // bubbling handler would never see a right-click inside the rename box.
        // handledEventsToo:true lets us catch it and offer Paste (text only).
        AddHandler(PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    // ---- Keyboard shortcuts (Ctrl+C / Ctrl+X / Ctrl+V / Delete) ----
    // Handled here (not via TreeView KeyBindings or menu InputGestures) so the
    // shortcuts never steal keys from a focused text input. Avalonia processes
    // ancestor KeyBindings before the focused control, so a TreeView KeyBinding made
    // Ctrl+V paste into the tree instead of the rename TextBox (AvaloniaUI/Avalonia#10902, #15771).

    private void OnTreeManagerKeyDown(object? sender, KeyEventArgs e)
    {
        // A focused TextBox (rename box, tree name, ...) handles these keys itself.
        if (IsTextInputFocused())
        {
            return;
        }

        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.C:
                    viewModel.CopyCommand.Execute(viewModel.SelectedNode);
                    e.Handled = true;
                    return;
                case Key.X:
                    viewModel.CutCommand.Execute(viewModel.SelectedNode);
                    e.Handled = true;
                    return;
                case Key.V:
                    viewModel.PasteCommand.Execute(viewModel.SelectedNode);
                    e.Handled = true;
                    return;
            }
        }
        else if (e.Key == Key.Delete)
        {
            viewModel.DeleteCommand.Execute(viewModel.SelectedNode);
            e.Handled = true;
        }
    }

    private bool IsTextInputFocused()
    {
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        return focused is TextBox;
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        // Right-click inside a TextBox (the inline rename box, the tree-name box, ...)
        // offers Paste of the clipboard TEXT as the value. Marking Handled also keeps
        // the row's node context menu from opening.
        if ((e.Source as Avalonia.Visual)?.FindAncestorOfType<TextBox>() is not { } textBox)
        {
            return;
        }

        _renameBox = textBox;
        RenamePastePopup.IsOpen = true;
        e.Handled = true;
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null || viewModel.SelectedTree is not { } tree)
        {
            return;
        }

        viewModel.OpenSavedTree(tree);
    }

    private void OnEditorTreeContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is TreeViewItem item && item.DataContext is TreeNodeViewModel)
        {
            item.PointerPressed -= OnTreeItemPointerPressed;
            item.PointerPressed += OnTreeItemPointerPressed;
        }
    }

    private void OnNodeContextMenuPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TreeNodeViewModel node } row)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        // Open the Copy / Cut / Paste / Delete menu as a popup at the pointer.
        viewModel.SelectedNode = node;
        viewModel.OpenNodeContextMenu(node);
        e.Handled = true;
    }

    private void OnNodeContextAction(object? sender, RoutedEventArgs e)
    {
        // The command ran first; close the popup like a native context menu.
        if (ViewModel is { } viewModel)
        {
            viewModel.IsNodeContextMenuOpen = false;
        }
    }

    private void OnAddNodeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: TreeNodeViewModel node } row)
        {
            return;
        }

        var container = row.FindAncestorOfType<TreeViewItem>();
        if (container is null)
        {
            return;
        }

        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        // Anchor the popup to this row; it stays fixed until Add / Cancel / outside click.
        NodePopup.PlacementTarget = container;
        viewModel.OpenNodePopup(node);
    }

    private void OnNameDoubleTapped(object? sender, TappedEventArgs e)
    {
        StartRename(sender);
    }

    private void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        StartRename(sender);
    }

    private void StartRename(object? sender)
    {
        if (sender is not Control { DataContext: TreeNodeViewModel node } row)
        {
            return;
        }

        _renameOriginals[node] = node.Name;
        node.IsEditing = true;

        // The rename box becomes visible synchronously with the INPC binding;
        // focus it and select the current name so the user can type immediately.
        var grid = row.FindAncestorOfType<Grid>();
        var renameBox = grid?.FindDescendantOfType<TextBox>();
        if (renameBox is not null)
        {
            renameBox.Focus();
            renameBox.SelectAll();
        }
    }

    private TextBox? _renameBox;

    private async void OnRenamePasteClick(object? sender, RoutedEventArgs e)
    {
        RenamePastePopup.IsOpen = false;

        if (_renameBox is not { } renameBox ||
            TopLevel.GetTopLevel(this) is not { Clipboard: { } clipboard })
        {
            return;
        }

        try
        {
            var text = await clipboard.TryGetTextAsync();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Paste only a plain-text NAME. If the clipboard holds a serialized
            // tree-node envelope (from Copy Node), extract just the node's name
            // instead of pasting the raw JSON as the new name.
            var name = Guardian.ProgramStation.Infrastructure.Services.TreeClipboardFormat.ExtractName(text);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            renameBox.Text = name;
            renameBox.CaretIndex = name.Length;
            renameBox.Focus();
        }
        catch
        {
            // Clipboard read failed; nothing to paste.
        }
    }

    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        EndRename(sender);
    }

    private void OnRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not Control { DataContext: TreeNodeViewModel node } textBox)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            EndRename(sender);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Cancel: restore the original name and leave edit mode.
            if (_renameOriginals.TryGetValue(node, out var original))
            {
                node.Name = original;
            }

            node.IsEditing = false;
            e.Handled = true;
        }
    }

    private void EndRename(object? sender)
    {
        if (sender is Control { DataContext: TreeNodeViewModel node })
        {
            _renameOriginals.Remove(node);
            node.IsEditing = false;
        }
    }

    private async void OnAddChild(object? sender, RoutedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        try
        {
            if (sender is not Control { DataContext: TreeNodeViewModel node })
            {
                return;
            }

            viewModel.SelectedNode = node;
            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }

            await viewModel.AddSubfolderAsync(window);
        }
        catch
        {
            // Ignore prompt failures.
        }
    }

    // ---- Drag & Drop: move nodes between parents ----

    // DataObject/DoDragDrop are obsolete in Avalonia 11.3+, but the replacement
    // DataTransfer API only supports string/byte[]/IStorageItem/Bitmap formats and
    // cannot carry arbitrary object references for in-app moves (AvaloniaUI/Avalonia#20097).
#pragma warning disable CS0618
    private const string DragFormat = "TreeNode";

    private void OnTreeItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TreeNodeViewModel node })
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var data = new DataObject();
            data.Set(DragFormat, node);
            DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }
    }

    private void OnTreeDragOver(object? sender, DragEventArgs e)
    {
        var dragged = e.Data.Get(DragFormat) as TreeNodeViewModel;
        var target = (e.Source as Control)?.DataContext as TreeNodeViewModel;

        e.DragEffects = dragged is not null && target is not null && CanMove(dragged, target)
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnTreeDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Get(DragFormat) is not TreeNodeViewModel dragged)
        {
            return;
        }

        var target = (e.Source as Control)?.DataContext as TreeNodeViewModel;
        if (target is null || !CanMove(dragged, target))
        {
            return;
        }

        if (dragged.Parent is null)
        {
            ViewModel?.RootNodes.Remove(dragged);
        }
        else
        {
            dragged.Parent.Children.Remove(dragged);
        }

        dragged.Parent = target;
        target.Children.Add(dragged);
    }
#pragma warning restore CS0618

    private static bool CanMove(TreeNodeViewModel dragged, TreeNodeViewModel target)
    {
        if (ReferenceEquals(dragged, target))
        {
            return false;
        }

        // Prevent dropping a node into its own descendant (would create a cycle).
        for (var node = target; node is not null; node = node.Parent)
        {
            if (ReferenceEquals(node, dragged))
            {
                return false;
            }
        }

        return true;
    }

    // ---- Panel window controls (minimize / maximize / close) ----

    private void OnMinimizeTreePreviewClick(object? sender, RoutedEventArgs e)
        => TreePreviewContent.IsVisible = !TreePreviewContent.IsVisible;

    private void OnMinimizeAsciiPreviewClick(object? sender, RoutedEventArgs e)
        => SetAsciiVisible(!(ViewModel?.IsAsciiVisible ?? true));

    private void OnCloseTreePreviewClick(object? sender, RoutedEventArgs e)
        => SetTreeVisible(false);

    private void OnCloseAsciiPreviewClick(object? sender, RoutedEventArgs e)
        => SetAsciiVisible(false);

    private void OnMaximizeTreePreviewClick(object? sender, RoutedEventArgs e)
    {
        TreePreviewPanel.IsVisible = true;
        AsciiPreviewPanel.IsVisible = false;
        TreePreviewContent.IsVisible = true;
        RestoreButtons.IsVisible = false;
        SetAsciiVisible(false);
    }

    private void OnMaximizeAsciiPreviewClick(object? sender, RoutedEventArgs e)
    {
        AsciiPreviewPanel.IsVisible = true;
        TreePreviewPanel.IsVisible = false;
        AsciiPreviewContent.IsVisible = true;
        RestoreButtons.IsVisible = false;
    }

    private void OnRestorePanelsClick(object? sender, RoutedEventArgs e)
    {
        TreePreviewPanel.IsVisible = true;
        AsciiPreviewPanel.IsVisible = true;
        TreePreviewContent.IsVisible = true;
        AsciiPreviewContent.IsVisible = true;
        RestoreButtons.IsVisible = false;
        SetTreeVisible(true);
        SetAsciiVisible(true);
    }

    private void SetTreeVisible(bool visible)
    {
        TreePreviewPanel.IsVisible = visible;
        RestoreButtons.IsVisible = !visible;

        // If Ascii is hidden too, the tree should fill; otherwise Ascii stretches.
        PreviewSplitGrid.ColumnDefinitions = (visible, ViewModel?.IsAsciiVisible ?? true) switch
        {
            (true, true) => new ColumnDefinitions("1.5*,1*"),
            (true, false) => new ColumnDefinitions("*,0"),
            (false, true) => new ColumnDefinitions("0,*"),
            _ => new ColumnDefinitions("1.5*,1*"),
        };
    }

    private void SetAsciiVisible(bool visible)
    {
        if (ViewModel is not null)
        {
            ViewModel.IsAsciiVisible = visible;
        }

        AsciiPreviewPanel.IsVisible = visible;
        RestoreButtons.IsVisible = !visible;

        // Fluid columns: Tree absorbs all space when Ascii is hidden.
        PreviewSplitGrid.ColumnDefinitions = visible
            ? new ColumnDefinitions("1.5*,1*")
            : new ColumnDefinitions("*,0");

        Console.WriteLine($"[Panels] Ascii visible: {visible}, Columns: {PreviewSplitGrid.ColumnDefinitions.Count}");
    }
}
