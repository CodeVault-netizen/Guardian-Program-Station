using System.Windows.Input;
using Avalonia.Media;
using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Infrastructure.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly ISettingsService _settingsService;
    private readonly IIndexingService _indexingService;
    private readonly IStorageService _storageService;
    private string _currentPage = "dashboard";
    private string _statusMessage = string.Empty;

    public MainViewModel(IServiceProvider services, ILocalizationService localization)
    {
        _localization = localization;
        _settingsService = services.GetRequiredService<ISettingsService>();
        _indexingService = services.GetRequiredService<IIndexingService>();
        _storageService = services.GetRequiredService<IStorageService>();
        _ = services.GetRequiredService<IndexingScheduler>();

        Dashboard = new DashboardViewModel(services, localization);
        Report = new FullReportViewModel(services, localization);
        TreeManager = new TreeManagerViewModel(
            services,
            localization,
            async () =>
            {
                await Report.LoadAsync();
                await Dashboard.LoadAsync();
            });
        Settings = new SettingsViewModel(services, localization, ApplyTheme);

        SwitchLanguageCommand = new RelayCommand(_ => SwitchLanguage());
        IndexNowCommand = new AsyncRelayCommand(async _ => await RunIndexingAsync());
        ShowDashboardCommand = new RelayCommand(_ => SetPage("dashboard"));
        ShowReportCommand = new RelayCommand(_ => SetPage("report"));
        ShowTreesCommand = new RelayCommand(_ => SetPage("trees"));
        ShowSettingsCommand = new RelayCommand(_ => SetPage("settings"));

        _localization.LanguageChanged += (_, _) => RefreshLocalized();
    }

    public DashboardViewModel Dashboard { get; }

    public FullReportViewModel Report { get; }

    public TreeManagerViewModel TreeManager { get; }

    public SettingsViewModel Settings { get; }

    public ICommand SwitchLanguageCommand { get; }

    public ICommand IndexNowCommand { get; }

    public ICommand ShowDashboardCommand { get; }

    public ICommand ShowReportCommand { get; }

    public ICommand ShowTreesCommand { get; }

    public ICommand ShowSettingsCommand { get; }

    public string AppTitle => _localization["AppTitle"];

    public string DashboardLabel => _localization["Dashboard"];

    public string Reports => _localization["Reports"];

    public string Trees => _localization["Trees"];

    public string SettingsLabel => _localization["Settings"];

    public string IndexNow => _localization["IndexNow"];

    public string LanguageButtonText => _localization.IsRtl ? "English" : "العربية";

    public string CurrentLanguageLabel => _localization.IsRtl ? "العربية" : "English";

    public ILocalizationService Localization => _localization;

    public FlowDirection Direction => _localization.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public bool IsDashboardVisible => _currentPage == "dashboard";

    public bool IsReportVisible => _currentPage == "report";

    public bool IsTreesVisible => _currentPage == "trees";

    public bool IsSettingsVisible => _currentPage == "settings";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task InitializeAsync()
    {
        await Dashboard.LoadAsync();
        await Report.LoadAsync();
        await TreeManager.LoadAsync();
        await Settings.LoadAsync();
    }

    private void SwitchLanguage()
    {
        var target = _localization.IsRtl ? "en" : "ar";
        _localization.SetLanguageAsync(target).GetAwaiter().GetResult();
        PersistSettings();
    }

    private async Task RunIndexingAsync()
    {
        var useCase = new IndexProgramsUseCase(_indexingService, _storageService, _settingsService);

        StatusMessage = _localization["IndexingRunning"];
        var found = await useCase.ExecuteAsync();
        await Report.LoadAsync();
        await Dashboard.LoadAsync();
        StatusMessage = _localization["IndexingDone"] + " " + found.Count;
    }

    private void ApplyTheme(string themeId)
    {
        if (Avalonia.Application.Current is not App app)
        {
            return;
        }

        var palette = Settings.AvailableThemes.FirstOrDefault(t => t.Id == themeId)?.Palette
                      ?? Settings.SelectedCustomTheme?.Palette;

        app.ApplyTheme(themeId, palette);
    }

    private void SetPage(string page)
    {
        _currentPage = page;
        OnPropertyChanged(nameof(IsDashboardVisible));
        OnPropertyChanged(nameof(IsReportVisible));
        OnPropertyChanged(nameof(IsTreesVisible));
        OnPropertyChanged(nameof(IsSettingsVisible));
    }

    private void RefreshLocalized()
    {
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(DashboardLabel));
        OnPropertyChanged(nameof(Reports));
        OnPropertyChanged(nameof(Trees));
        OnPropertyChanged(nameof(SettingsLabel));
        OnPropertyChanged(nameof(IndexNow));
        OnPropertyChanged(nameof(LanguageButtonText));
        OnPropertyChanged(nameof(CurrentLanguageLabel));
        OnPropertyChanged(nameof(Direction));

        Dashboard.RefreshLocalized();
        Report.RefreshLocalized();
        TreeManager.RefreshLocalized();
        Settings.RefreshLocalized();
    }

    private void PersistSettings()
    {
        _ = Task.Run(async () =>
        {
            var settings = await _settingsService.LoadAsync();
            settings.Language = _localization.CurrentLanguage;
            await _settingsService.SaveAsync(settings);
        });
    }
}
