using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Guardian.ProgramStation.UI.Views;

public enum SavePromptResult
{
    Cancel = 0,
    Save,
    DontSave,
}

public partial class SavePromptWindow : Window
{
    public SavePromptWindow()
    {
        InitializeComponent();
    }

    public SavePromptWindow(string title, string message, string saveText, string dontSaveText, string cancelText)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        SaveButton.Content = saveText;
        DontSaveButton.Content = dontSaveText;
        CancelButton.Content = cancelText;
    }

    private void OnSave(object? sender, RoutedEventArgs e) => Close(SavePromptResult.Save);

    private void OnDontSave(object? sender, RoutedEventArgs e) => Close(SavePromptResult.DontSave);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(SavePromptResult.Cancel);
}
