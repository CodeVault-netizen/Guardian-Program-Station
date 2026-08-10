using Avalonia.Controls;
using Avalonia.Interactivity;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class TreeManagerView : UserControl
{
    private TreeManagerViewModel? ViewModel => DataContext as TreeManagerViewModel;

    public TreeManagerView()
    {
        InitializeComponent();
    }

    public TreeManagerView(TreeManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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

    private async void OnDeleteTree(object? sender, RoutedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        try
        {
            if (sender is not Control { DataContext: TreeModel tree })
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }

            var dialog = new ConfirmWindow(
                viewModel.DeleteTreeLabel,
                viewModel.ConfirmDeleteTreeLabel,
                viewModel.ConfirmLabel,
                viewModel.CancelLabel);

            var confirmed = await dialog.ShowDialog<bool>(window);
            if (confirmed)
            {
                await viewModel.DeleteTreeAsync(tree);
            }
        }
        catch
        {
            // Keep the app alive on storage failures.
        }
    }
}
