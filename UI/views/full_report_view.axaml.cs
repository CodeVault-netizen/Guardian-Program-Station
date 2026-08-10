using Avalonia.Controls;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class FullReportView : UserControl
{
    public FullReportView()
    {
        InitializeComponent();
    }

    public FullReportView(FullReportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
