using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Guardian.ProgramStation.UI.ViewModels;

public sealed class TreeNodeViewModel : ObservableObject
{
    private readonly ITreeNodeHost _owner;
    private string _name = string.Empty;

    public TreeNodeViewModel(ITreeNodeHost owner)
    {
        _owner = owner;
    }

    public TreeNodeViewModel? Parent { get; set; }

    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public ICommand RemoveCommand => _owner.RemoveNodeCommand;

    public string AddLabel => _owner.AddLabel;

    public string RemoveLabel => _owner.RemoveLabel;
}
