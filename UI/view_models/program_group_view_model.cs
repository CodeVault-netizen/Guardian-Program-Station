using System.Collections.ObjectModel;
using System.Windows.Input;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.UI.ViewModels;

/// <summary>A collapsible group of programs sharing the same parent folder/section.</summary>
public sealed class ProgramGroupViewModel : ObservableObject
{
    private bool _isExpanded = true;

    public ProgramGroupViewModel(string section)
    {
        Section = section;
        ToggleCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
    }

    public string Section { get; }

    public ObservableCollection<ProgramModel> Programs { get; } = new();

    public ICommand ToggleCommand { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(IsCollapsed));
            }
        }
    }

    /// <summary>Inverse of <see cref="IsExpanded"/> for the collapse arrow glyph.</summary>
    public bool IsCollapsed => !IsExpanded;

    public int Count => Programs.Count;
}
