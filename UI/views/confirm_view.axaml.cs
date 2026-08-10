using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Guardian.ProgramStation.UI.Views;

public partial class ConfirmWindow : Window
{
    public bool Result { get; private set; }

    public ConfirmWindow()
    {
        InitializeComponent();
    }

    public ConfirmWindow(string title, string message, string confirmText, string cancelText)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
