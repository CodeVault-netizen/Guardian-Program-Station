using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class TreeNodeViewModel : ObservableObject
{
    private readonly ITreeNodeHost _owner;
    private string _name = string.Empty;
    private string _nodeType = "folder";
    private bool _isEditing;
    private DateTime _createdAt = DateTime.UtcNow;

    public TreeNodeViewModel(ITreeNodeHost owner)
    {
        _owner = owner;
        Children.CollectionChanged += (_, _) => _owner.OnTreeChanged();
    }

    public TreeNodeViewModel? Parent { get; set; }

    public bool IsRoot => Parent is null;

    /// <summary>True while the row is in inline-rename mode.</summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsNotEditing));
            }
        }
    }

    public bool IsNotEditing => !_isEditing;

    public bool IsExpanded { get; set; } = true;

    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _owner.OnTreeChanged();
            }
        }
    }

    /// <summary>Creation time, used by the Creation Time sort options.</summary>
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    /// <summary>Node type: "folder" or "file".</summary>
    public string NodeType
    {
        get => _nodeType;
        set
        {
            if (SetProperty(ref _nodeType, value))
            {
                OnPropertyChanged(nameof(IsFolder));
                OnPropertyChanged(nameof(IsFile));
            }
        }
    }

    public bool IsFolder => string.Equals(_nodeType, "folder", StringComparison.OrdinalIgnoreCase);

    public bool IsFile => string.Equals(_nodeType, "file", StringComparison.OrdinalIgnoreCase);

    public ICommand RemoveCommand => _owner.RemoveNodeCommand;

    public ICommand CopyCommand => _owner.CopyCommand;

    public ICommand CutCommand => _owner.CutCommand;

    public ICommand PasteCommand => _owner.PasteCommand;

    public ICommand DeleteCommand => _owner.DeleteCommand;

    public ICommand CopyNameCommand => _owner.CopyNameCommand;

    public ICommand PasteNameCommand => _owner.PasteNameCommand;

    public string AddLabel => _owner.AddLabel;

    public string RemoveLabel => _owner.RemoveLabel;

    public string RenameLabel => _owner.RenameLabel;

    public string CopyLabel => _owner.CopyLabel;

    public string CutLabel => _owner.CutLabel;

    public string PasteLabel => _owner.PasteLabel;

    public string DeleteLabel => _owner.DeleteLabel;

    public string CopyNameLabel => _owner.CopyNameLabel;

    public string PasteNameLabel => _owner.PasteNameLabel;
}
