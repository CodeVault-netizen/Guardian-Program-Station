using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localization;
    private readonly Action<string> _applyTheme;
    private ThemeDefinition? _selectedTheme;
    private string _selectedLanguageDisplay = "English";
    private bool _autoIndexEnabled;
    private string _scheduleInterval = "daily";
    private string _newFolderPath = string.Empty;
    private string _customBackground = "#2C2A29";
    private string _customElement = "#4B4A48";
    private string _customAdditional = "#3A3837";
    private string _customBorder = "#7A7B7A";
    private string _customPrimaryText = "#A8A8A8";
    private string _customAccentText = "#D3D3D3";

    public SettingsViewModel(IServiceProvider services, ILocalizationService localization, Action<string> applyTheme)
    {
        _settingsService = services.GetRequiredService<ISettingsService>();
        _themeService = services.GetRequiredService<IThemeService>();
        _localization = localization;
        _applyTheme = applyTheme;

        AddFolderCommand = new AsyncRelayCommand(async owner => await AddFolderAsync(owner));
        RemoveFolderCommand = new RelayCommand(parameter => RemoveFolder(parameter as string));
        SaveCustomThemeCommand = new RelayCommand(_ => SaveCustomTheme());

        _localization.LanguageChanged += (_, _) =>
        {
            _selectedLanguageDisplay = _localization.IsRtl ? "العربية" : "English";
            OnPropertyChanged(nameof(SelectedLanguageDisplay));
            RefreshLocalized();
        };
    }

    public ObservableCollection<ThemeDefinition> AvailableThemes { get; } = new();

    public ObservableCollection<string> FavoriteFolders { get; } = new();

    public ObservableCollection<string> LanguageOptions { get; } = new() { "English", "العربية" };

    public ObservableCollection<string> ScheduleOptions { get; } = new() { "Hourly", "Daily", "Weekly" };

    public string ThemeLabel => _localization["Theme"];

    public string LanguageLabel => _localization["Language"];

    public string AutoIndexLabel => _localization["AutoIndex"];

    public string ScheduleLabel => _localization["Schedule"];

    public string FavoriteFoldersLabel => _localization["FavoriteFolders"];

    public string AddFolderLabel => _localization["AddFolder"];

    public string RemoveFolderLabel => _localization["RemoveFolder"];

    public string CustomThemeLabel => _localization["CustomTheme"];

    public string SaveCustomThemeLabel => _localization["SaveCustomTheme"];

    public string BackgroundLabel => _localization["Background"];

    public string ElementLabel => _localization["Element"];

    public string AdditionalLabel => _localization["Additional"];

    public string BorderLabel => _localization["Border"];

    public string PrimaryTextLabel => _localization["PrimaryText"];

    public string AccentTextLabel => _localization["AccentText"];

    public string SaveSettingsLabel => _localization["SaveSettings"];

    public FlowDirection Direction => _localization.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public ICommand AddFolderCommand { get; }

    public ICommand RemoveFolderCommand { get; }

    public ICommand SaveCustomThemeCommand { get; }

    public ThemeDefinition? SelectedCustomTheme { get; private set; }

    public ThemeDefinition? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (!SetProperty(ref _selectedTheme, value) || value is null)
            {
                return;
            }

            _applyTheme(value.Id);
            Persist(s => s.ThemeId = value.Id);
        }
    }

    public string SelectedLanguageDisplay
    {
        get => _selectedLanguageDisplay;
        set
        {
            if (!SetProperty(ref _selectedLanguageDisplay, value))
            {
                return;
            }

            var code = value == "العربية" ? "ar" : "en";
            _localization.SetLanguageAsync(code).GetAwaiter().GetResult();
            Persist(s => s.Language = code);
        }
    }

    public bool AutoIndexEnabled
    {
        get => _autoIndexEnabled;
        set
        {
            if (SetProperty(ref _autoIndexEnabled, value))
            {
                Persist(s => s.AutoIndexEnabled = value);
            }
        }
    }

    public string ScheduleIntervalDisplay
    {
        get => _scheduleInterval switch
        {
            "hourly" => "Hourly",
            "weekly" => "Weekly",
            _ => "Daily",
        };
        set
        {
            var interval = value switch
            {
                "Hourly" => "hourly",
                "Weekly" => "weekly",
                _ => "daily",
            };

            if (SetProperty(ref _scheduleInterval, interval))
            {
                Persist(s => s.ScheduleInterval = interval);
            }
        }
    }

    public string NewFolderPath
    {
        get => _newFolderPath;
        set => SetProperty(ref _newFolderPath, value);
    }

    public string CustomBackground
    {
        get => _customBackground;
        set => SetProperty(ref _customBackground, value);
    }

    public string CustomElement
    {
        get => _customElement;
        set => SetProperty(ref _customElement, value);
    }

    public string CustomAdditional
    {
        get => _customAdditional;
        set => SetProperty(ref _customAdditional, value);
    }

    public string CustomBorder
    {
        get => _customBorder;
        set => SetProperty(ref _customBorder, value);
    }

    public string CustomPrimaryText
    {
        get => _customPrimaryText;
        set => SetProperty(ref _customPrimaryText, value);
    }

    public string CustomAccentText
    {
        get => _customAccentText;
        set => SetProperty(ref _customAccentText, value);
    }

    /// <summary>
    /// Persists the current in-memory settings to storage. Used by the dialog's
    /// "Save Settings" button before the window closes.
    /// </summary>
    public async Task SaveAsync()
    {
        var settings = await _settingsService.LoadAsync();
        settings.ThemeId = SelectedTheme?.Id ?? "dark";
        settings.Language = _localization.CurrentLanguage;
        settings.AutoIndexEnabled = AutoIndexEnabled;
        settings.ScheduleInterval = _scheduleInterval;
        settings.FavoriteFolders = FavoriteFolders.ToList();
        await _settingsService.SaveAsync(settings);
    }

    public async Task LoadAsync()
    {
        var themes = await _themeService.GetAvailableThemesAsync();
        AvailableThemes.Clear();
        foreach (var theme in themes)
        {
            AvailableThemes.Add(theme);
        }

        SelectedCustomTheme = themes.FirstOrDefault(t => !t.IsBuiltIn);

        var settings = await _settingsService.LoadAsync();

        _autoIndexEnabled = settings.AutoIndexEnabled;
        _scheduleInterval = settings.ScheduleInterval;
        _selectedLanguageDisplay = settings.Language == "ar" ? "العربية" : "English";
        _selectedTheme = AvailableThemes.FirstOrDefault(t => t.Id == settings.ThemeId)
                         ?? AvailableThemes.FirstOrDefault();

        OnPropertyChanged(nameof(AutoIndexEnabled));
        OnPropertyChanged(nameof(ScheduleIntervalDisplay));
        OnPropertyChanged(nameof(SelectedLanguageDisplay));
        OnPropertyChanged(nameof(SelectedTheme));

        FavoriteFolders.Clear();
        foreach (var folder in settings.FavoriteFolders)
        {
            FavoriteFolders.Add(folder);
        }
    }

    private async Task AddFolderAsync(object? owner)
    {
        if (owner is not Avalonia.Controls.Window window)
        {
            return;
        }

        var folder = await window.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = _localization["AddFolder"],
            AllowMultiple = false,
        });

        if (folder.Count == 0)
        {
            return;
        }

        var path = folder[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        FavoriteFolders.Add(path);
        Persist(s => s.FavoriteFolders = FavoriteFolders.ToList());
    }

    private void RemoveFolder(string? folder)
    {
        if (folder is null)
        {
            return;
        }

        FavoriteFolders.Remove(folder);
        Persist(s => s.FavoriteFolders = FavoriteFolders.ToList());
    }

    private async void SaveCustomTheme()
    {
        var theme = new ThemeDefinition
        {
            Id = SelectedCustomTheme?.Id ?? "custom",
            Name = SelectedCustomTheme?.Name ?? "Custom",
            IsBuiltIn = false,
            Palette = new ThemePalette
            {
                Background = _customBackground,
                ElementBackground = _customElement,
                AdditionalBackground = _customAdditional,
                Border = _customBorder,
                PrimaryText = _customPrimaryText,
                AccentText = _customAccentText,
            },
        };

        await _themeService.SaveCustomThemeAsync(theme);
        SelectedCustomTheme = theme;

        var existing = AvailableThemes.FirstOrDefault(t => t.Id == theme.Id);
        if (existing is not null)
        {
            AvailableThemes[AvailableThemes.IndexOf(existing)] = theme;
        }
        else
        {
            AvailableThemes.Add(theme);
        }

        _selectedTheme = theme;
        OnPropertyChanged(nameof(SelectedTheme));
        _applyTheme("custom");
        Persist(s => s.ThemeId = "custom");
    }

    public void RefreshLocalized()
    {
        OnPropertyChanged(nameof(SelectedLanguageDisplay));
        OnPropertyChanged(nameof(ThemeLabel));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(AutoIndexLabel));
        OnPropertyChanged(nameof(ScheduleLabel));
        OnPropertyChanged(nameof(FavoriteFoldersLabel));
        OnPropertyChanged(nameof(AddFolderLabel));
        OnPropertyChanged(nameof(RemoveFolderLabel));
        OnPropertyChanged(nameof(CustomThemeLabel));
        OnPropertyChanged(nameof(SaveCustomThemeLabel));
        OnPropertyChanged(nameof(BackgroundLabel));
        OnPropertyChanged(nameof(ElementLabel));
        OnPropertyChanged(nameof(AdditionalLabel));
        OnPropertyChanged(nameof(BorderLabel));
        OnPropertyChanged(nameof(PrimaryTextLabel));
        OnPropertyChanged(nameof(AccentTextLabel));
        OnPropertyChanged(nameof(SaveSettingsLabel));
        OnPropertyChanged(nameof(Direction));
    }

    private void Persist(Action<SettingsModel> update)
    {
        _ = Task.Run(async () =>
        {
            var settings = await _settingsService.LoadAsync();
            update(settings);
            await _settingsService.SaveAsync(settings);
        });
    }
}
