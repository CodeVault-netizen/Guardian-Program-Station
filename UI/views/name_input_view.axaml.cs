using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Guardian.ProgramStation.UI.Views;

public partial class NameInputWindow : Window
{
    public string? Result { get; private set; }

    public NameInputWindow()
    {
        InitializeComponent();
    }

    public NameInputWindow(string title, string prompt, string confirmText, string cancelText)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        Result = InputBox.Text;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
