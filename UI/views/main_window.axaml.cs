using Avalonia.Controls;
using Avalonia.Interactivity;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel? _viewModel;
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Closing += OnClosing;

        DashboardHost.Content = new DashboardView(viewModel.Dashboard);
        ReportHost.Content = new FullReportView(viewModel.Report);
        TreeHost.Content = new TreeManagerView(viewModel.TreeManager);

        viewModel.Report.EditRequested += async dto =>
        {
            var dialog = new ProgramEntryWindow(new ProgramEntryViewModel(viewModel.Localization, dto));
            var saved = await dialog.ShowDialog<bool>(this);

            if (saved)
            {
                await viewModel.Report.SaveAsync(dialog.ViewModel.Result);
            }
        };

        viewModel.Report.CloseRequested += () => viewModel.ShowTreesCommand.Execute(null);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || _viewModel is null || !_viewModel.TreeManager.HasUnsavedChanges)
        {
            return;
        }

        // Ask the user before closing; keep the window open until they decide.
        e.Cancel = true;

        var dialog = new SavePromptWindow(
            _viewModel.Localization["Save"],
            _viewModel.Localization["SaveChangesPrompt"],
            _viewModel.Localization["Save"],
            _viewModel.Localization["DontSave"],
            _viewModel.Localization["Cancel"]);

        var choice = await dialog.ShowDialog<SavePromptResult>(this);
        switch (choice)
        {
            case SavePromptResult.Save:
                var saved = await _viewModel.TreeManager.TrySaveCurrentTreeAsync(this);
                if (!saved)
                {
                    // The user cancelled the location dialog: keep the app open.
                    return;
                }

                break;
            case SavePromptResult.DontSave:
                break;
            default:
                // Cancel (or the dialog was closed with ✕): keep the app open.
                return;
        }

        _closeConfirmed = true;
        Close();
    }

    private void OnSortClick(object? sender, RoutedEventArgs e)
    {
        // Opens the Sort options popup (Popup + IsOpen binding, the app's proven pattern).
        if (DataContext is MainViewModel vm)
        {
            vm.TreeManager.IsSortMenuOpen = true;
        }
    }
}
