using Avalonia.Media;
using Guardian.ProgramStation.Application.Dtos;
using Guardian.ProgramStation.Application.Interfaces;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class ProgramEntryViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly bool _isEditing;
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _version = string.Empty;
    private string _link = string.Empty;
    private string _license = string.Empty;
    private string _notes = string.Empty;

    public ProgramEntryViewModel(ILocalizationService localization, ProgramEntryDto? initial = null)
    {
        _localization = localization;
        _isEditing = initial is not null;

        if (initial is not null)
        {
            _id = initial.Id ?? string.Empty;
            _name = initial.Name;
            _version = initial.Version;
            _link = initial.Link;
            _license = initial.License;
            _notes = initial.Notes;
        }
    }

    public string Title => _localization[_isEditing ? "EditProgram" : "AddProgram"];

    public FlowDirection Direction => _localization.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public string NameLabel => _localization["Name"];

    public string VersionLabel => _localization["Version"];

    public string LinkLabel => _localization["Link"];

    public string LicenseLabel => _localization["License"];

    public string NotesLabel => _localization["Notes"];

    public string SaveText => _localization["Save"];

    public string CancelText => _localization["Cancel"];

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    public string Link
    {
        get => _link;
        set => SetProperty(ref _link, value);
    }

    public string License
    {
        get => _license;
        set => SetProperty(ref _license, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool Saved { get; private set; }

    public ProgramEntryDto Result => new()
    {
        Id = _id,
        Name = _name,
        Version = _version,
        Link = _link,
        License = _license,
        Notes = _notes,
    };

    public void Submit()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            return;
        }

        Saved = true;
    }
}
