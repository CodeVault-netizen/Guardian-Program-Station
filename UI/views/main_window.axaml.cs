using Avalonia.Controls;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        DashboardHost.Content = new DashboardView(viewModel.Dashboard);
        ReportHost.Content = new FullReportView(viewModel.Report);
        TreeHost.Content = new TreeManagerView(viewModel.TreeManager);
        SettingsHost.Content = new SettingsView(viewModel.Settings);

        viewModel.Report.EditRequested += async dto =>
        {
            var dialog = new ProgramEntryWindow(new ProgramEntryViewModel(viewModel.Localization, dto));
            var saved = await dialog.ShowDialog<bool>(this);

            if (saved)
            {
                await viewModel.Report.SaveAsync(dialog.ViewModel.Result);
            }
        };
    }
}
