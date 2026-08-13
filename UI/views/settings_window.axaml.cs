using Avalonia.Controls;
using Avalonia.Interactivity;
using Guardian.ProgramStation.UI.ViewModels;

namespace Guardian.ProgramStation.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.SaveAsync();
        }

        Close();
    }
}
