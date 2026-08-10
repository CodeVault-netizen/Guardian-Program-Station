using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Guardian.ProgramStation.UI.Themes;

public partial class DarkTheme : Styles
{
    public DarkTheme()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
