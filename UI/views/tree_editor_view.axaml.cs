using Avalonia.Controls;
using Avalonia.Interactivity;
using Guardian.ProgramStation.Infrastructure.Services;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class TreeEditorWindow : Window
{
    public TreeEditorViewModel ViewModel { get; }

    public TreeEditorWindow()
    {
        InitializeComponent();
        ViewModel = new TreeEditorViewModel(new TreeStorageService(), new LocalizationService());
        DataContext = ViewModel;
    }

    public TreeEditorWindow(TreeEditorViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
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

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.SaveAsync();
            Close(ViewModel.Saved);
        }
        catch
        {
            // Keep the dialog open so the user can retry.
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

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
