using Avalonia.Controls;
using Avalonia.Interactivity;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class ProgramEntryWindow : Window
{
    public ProgramEntryViewModel ViewModel { get; }

    public ProgramEntryWindow()
    {
        InitializeComponent();
        ViewModel = new ProgramEntryViewModel(
            new Guardian.ProgramStation.Infrastructure.Services.LocalizationService());
        DataContext = ViewModel;
    }

    public ProgramEntryWindow(ProgramEntryViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        ViewModel.Submit();

        if (ViewModel.Saved)
        {
            Close(true);
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
