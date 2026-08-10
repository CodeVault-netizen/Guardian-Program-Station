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
using Guardian.ProgramStation.UI.Themes;
using Guardian.ProgramStation.UI.ViewModels;
using Guardian.ProgramStation.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

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

        _services = DependencyInjection.BuildServiceProvider();

        var localization = _services.GetRequiredService<ILocalizationService>();
        var settingsService = _services.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadAsync().GetAwaiter().GetResult();

        if (!string.Equals(settings.Language, "en", StringComparison.OrdinalIgnoreCase))
        {
            localization.SetLanguageAsync(settings.Language).GetAwaiter().GetResult();
        }

        ApplyTheme(settings.ThemeId);

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
        IStyle theme = themeId switch
        {
            "light" => new LightTheme(),
            "custom" => new CustomTheme(),
            _ => new DarkTheme(),
        };

        RequestedThemeVariant = themeId switch
        {
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Dark,
        };

        Styles.Clear();
        Styles.Add(theme);

        if (palette is not null)
        {
            SetPalette(palette);
        }
    }

    private static void SetPalette(ThemePalette palette)
    {
        SetBrush("GpsBackground", palette.Background);
        SetBrush("GpsElement", palette.ElementBackground);
        SetBrush("GpsAdditional", palette.AdditionalBackground);
        SetBrush("GpsBorder", palette.Border);
        SetBrush("GpsPrimaryText", palette.PrimaryText);
        SetBrush("GpsAccentText", palette.AccentText);
        SetBrush("GpsAccent", palette.AccentText);
    }

    private static void SetBrush(string key, string? color)
    {
        if (color is not null && Color.TryParse(color, out var parsed))
        {
            Avalonia.Application.Current!.Resources[key] = new SolidColorBrush(parsed);
        }
    }
}
