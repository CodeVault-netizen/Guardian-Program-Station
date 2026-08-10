using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Guardian.ProgramStation.UI.Themes;

public partial class LightTheme : Styles
{
    public LightTheme()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
