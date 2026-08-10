using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Guardian.ProgramStation.UI.Themes;

public partial class CustomTheme : Styles
{
    public CustomTheme()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
