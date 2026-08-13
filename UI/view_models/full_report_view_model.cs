using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class FullReportViewModel : ObservableObject
{
    private readonly IStorageService _storage;
    private readonly ILocalizationService _localization;
    private readonly ExportReportUseCase _export;
    private List<ProgramModel> _all = new();
    private string _searchText = string.Empty;
    private ExecutableType? _typeFilter;
    private bool _sortAscending = true;
    private ProgramModel? _selectedProgram;

    public FullReportViewModel(IServiceProvider services, ILocalizationService localization)
    {
        _storage = services.GetRequiredService<IStorageService>();
        _localization = localization;
        _export = services.GetRequiredService<ExportReportUseCase>();

        CloseReportsCommand = new RelayCommand(_ => CloseRequested?.Invoke());
        AddProgramCommand = new RelayCommand(_ => EditRequested?.Invoke(null));
        EditProgramCommand = new RelayCommand(_ => EditRequested?.Invoke(_selectedProgram is null ? null : ToDto(_selectedProgram)), _ => _selectedProgram is not null);
        DeleteProgramCommand = new AsyncRelayCommand(async _ =>
        {
            if (_selectedProgram is null)
            {
                return;
            }

            var program = _selectedProgram;
            await _storage.DeleteProgramAsync(program.Id);
            await LoadAsync();
        }, _ => _selectedProgram is not null);

        ExportCsvCommand = new AsyncRelayCommand(async owner => await _export.ExportToCsvAsync(await PickExportPath(owner, "csv")));
        ExportJsonCommand = new AsyncRelayCommand(async owner => await _export.ExportToJsonAsync(await PickExportPath(owner, "json")));

        SetTypeFilterCommand = new RelayCommand(parameter =>
        {
            _typeFilter = parameter switch
            {
                "Windows" => ExecutableType.Windows,
                "Linux" => ExecutableType.Linux,
                "MacOS" => ExecutableType.MacOs,
                _ => null,
            };
            ApplyFilter();
        });

        ToggleSortCommand = new RelayCommand(_ => ToggleSort());
    }

    public event Action<ProgramEntryDto?>? EditRequested;

    /// <summary>Raised when the user closes the reports page.</summary>
    public event Action? CloseRequested;

    public ObservableCollection<ProgramGroupViewModel> Groups { get; } = new();

    /// <summary>Flat, observable list of every program (what the ListBox binds to).</summary>
    public ObservableCollection<ProgramModel> Programs { get; } = new();

    /// <summary>Visible diagnostics: number of groups currently bound.</summary>
    public int GroupsCount => Groups.Count;

    public IStorageService Storage => _storage;

    public ICommand CloseReportsCommand { get; }

    public ICommand AddProgramCommand { get; }

    public RelayCommand EditProgramCommand { get; }

    public AsyncRelayCommand DeleteProgramCommand { get; }

    public ICommand ExportCsvCommand { get; }

    public ICommand ExportJsonCommand { get; }

    public ICommand SetTypeFilterCommand { get; }

    public ICommand ToggleSortCommand { get; }

    public ProgramModel? SelectedProgram
    {
        get => _selectedProgram;
        set
        {
            if (SetProperty(ref _selectedProgram, value))
            {
                EditProgramCommand.RaiseCanExecuteChanged();
                DeleteProgramCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string NameColumn => _localization["Name"];

    public string VersionColumn => _localization["Version"];

    public string PathColumn => _localization["Path"];

    public string LinkColumn => _localization["Link"];

    public string LicenseColumn => _localization["License"];

    public string NotesColumn => _localization["Notes"];

    public string TypeColumn => _localization["Type"];

    public string AddLabel => _localization["Add"];

    public string EditLabel => _localization["Edit"];

    public string DeleteLabel => _localization["Delete"];

    public string ExportCsvLabel => _localization["ExportCsv"];

    public string ExportJsonLabel => _localization["ExportJson"];

    public string CloseLabel => _localization["Close"];

    public string AllLabel => _localization["All"];

    public string WindowsLabel => _localization["Windows"];

    public string LinuxLabel => _localization["Linux"];

    public string MacOsLabel => _localization["MacOS"];

    public string SearchPlaceholder => _localization["Search"];

    public string SortIndicator => _sortAscending ? "Name ▲" : "Name ▼";

    public async Task LoadAsync()
    {
        _all = (await _storage.LoadProgramsAsync()).ToList();
        ApplyFilter();
        Console.WriteLine($"[Reports] Loaded {_all.Count} programs, Groups count: {Groups.Count}");
    }

    public async Task SaveAsync(ProgramEntryDto dto)
    {
        var program = _all.FirstOrDefault(p => p.Id == dto.Id)
                      ?? new ProgramModel { Id = dto.Id ?? Guid.NewGuid().ToString("N") };

        program.Name = dto.Name;
        program.Version = dto.Version;
        program.Link = dto.Link;
        program.License = dto.License;
        program.Notes = dto.Notes;

        await _storage.SaveProgramAsync(program);
        await LoadAsync();
    }

    public void ToggleSort()
    {
        _sortAscending = !_sortAscending;
        OnPropertyChanged(nameof(SortIndicator));
        ApplyFilter();
    }

    public void RefreshLocalized()
    {
        OnPropertyChanged(nameof(NameColumn));
        OnPropertyChanged(nameof(VersionColumn));
        OnPropertyChanged(nameof(PathColumn));
        OnPropertyChanged(nameof(LinkColumn));
        OnPropertyChanged(nameof(LicenseColumn));
        OnPropertyChanged(nameof(NotesColumn));
        OnPropertyChanged(nameof(TypeColumn));
        OnPropertyChanged(nameof(AddLabel));
        OnPropertyChanged(nameof(EditLabel));
        OnPropertyChanged(nameof(DeleteLabel));
        OnPropertyChanged(nameof(ExportCsvLabel));
        OnPropertyChanged(nameof(ExportJsonLabel));
        OnPropertyChanged(nameof(AllLabel));
        OnPropertyChanged(nameof(WindowsLabel));
        OnPropertyChanged(nameof(LinuxLabel));
        OnPropertyChanged(nameof(MacOsLabel));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(SortIndicator));
    }

    private void ApplyFilter()
    {
        var filtered = _all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || p.Path.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (_typeFilter is { } type)
        {
            filtered = filtered.Where(p => p.ExecutableType == type);
        }

        filtered = _sortAscending
            ? filtered.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            : filtered.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase);

        Groups.Clear();
        Programs.Clear();
        foreach (var group in filtered
                     .GroupBy(p => p.ParentSection, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var viewModel = new ProgramGroupViewModel(group.Key);
            var index = 0;
            foreach (var program in group)
            {
                program.IsAlternate = index++ % 2 == 1;
                viewModel.Programs.Add(program);
                Programs.Add(program);
            }

            Groups.Add(viewModel);
        }

        OnPropertyChanged(nameof(GroupsCount));
    }

    private static ProgramEntryDto ToDto(ProgramModel program) => new()
    {
        Id = program.Id,
        Name = program.Name,
        Version = program.Version,
        Link = program.Link,
        License = program.License,
        Notes = program.Notes,
    };

    private async Task<string> PickExportPath(object? owner, string extension)
    {
        if (owner is not Avalonia.Controls.Window window)
        {
            return string.Empty;
        }

        var file = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            SuggestedFileName = $"programs_report.{extension}",
            DefaultExtension = extension,
            FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType($"{extension.ToUpperInvariant()} file") },
        });

        return file?.TryGetLocalPath() ?? string.Empty;
    }
}
