using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Infrastructure.Scheduling;
using Guardian.ProgramStation.Infrastructure.Services;
using Guardian.ProgramStation.Kernel;
using Guardian.ProgramStation.UI.Services;
using Guardian.ProgramStation.UI.Themes;
using Guardian.ProgramStation.UI.ViewModels;
using Guardian.ProgramStation.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;
    private bool _themeStylesLoaded;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashLogger.Log(e.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception"));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLogger.Log(e.Exception);
            e.SetObserved();
        };
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            CrashLogger.Log(e.Exception);
            e.Handled = true;
        };

        _services = DependencyInjection.BuildServiceProvider(services =>
            services.AddSingleton<IClipboardService, SystemClipboardService>());

        var localization = _services.GetRequiredService<ILocalizationService>();
        var settingsService = _services.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadAsync().GetAwaiter().GetResult();

        if (!string.Equals(settings.Language, "en", StringComparison.OrdinalIgnoreCase))
        {
            localization.SetLanguageAsync(settings.Language).GetAwaiter().GetResult();
        }

        var themeService = _services.GetRequiredService<IThemeService>();
        var theme = themeService.GetThemeAsync(settings.ThemeId).GetAwaiter().GetResult();
        ApplyTheme(settings.ThemeId, theme.Palette);

        var scheduler = _services.GetRequiredService<IndexingScheduler>();
        scheduler.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = new MainViewModel(_services, localization);
            desktop.MainWindow = new MainWindow(mainViewModel);
            _ = mainViewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ApplyTheme(string themeId, ThemePalette? palette = null)
    {
        // All built-in themes (Dark/Light/Custom) share identical control
        // templates; only the color palette differs. Load the template set
        // once and swap only the brushes on every switch, so changing themes
        // never triggers a full style re-evaluation of the whole UI.
        if (!_themeStylesLoaded)
        {
            Styles.Clear();
            Styles.Add(new DarkTheme());
            _themeStylesLoaded = true;
        }

        RequestedThemeVariant = themeId switch
        {
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Dark,
        };

        if (palette is not null)
        {
            var accent = themeId switch
            {
                "light" => "#8A7A63",
                _ => "#5B9BD5",
            };

            SetPalette(palette, accent);
        }
    }

    private static void SetPalette(ThemePalette palette, string accent)
    {
        SetBrush("GpsBackground", palette.Background);
        SetBrush("GpsElement", palette.ElementBackground);
        SetBrush("GpsAdditional", palette.AdditionalBackground);
        SetBrush("GpsBorder", palette.Border);
        SetBrush("GpsPrimaryText", palette.PrimaryText);
        SetBrush("GpsAccentText", palette.AccentText);
        SetBrush("GpsAccent", accent);
    }

    private static void SetBrush(string key, string? color)
    {
        if (color is not null && Color.TryParse(color, out var parsed))
        {
            Avalonia.Application.Current!.Resources[key] = new SolidColorBrush(parsed);
        }
    }
}
