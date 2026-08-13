using System.Windows.Input;

namespace Guardian.ProgramStation.UI.ViewModels;

public interface ITreeNodeHost
{
    ICommand RemoveNodeCommand { get; }

    ICommand CopyCommand { get; }

    ICommand CutCommand { get; }

    ICommand PasteCommand { get; }

    ICommand DeleteCommand { get; }

    /// <summary>Copies only the node's name as plain text.</summary>
    ICommand CopyNameCommand { get; }

    /// <summary>Pastes the clipboard's plain text as the node's name only.</summary>
    ICommand PasteNameCommand { get; }

    string AddLabel { get; }

    string RemoveLabel { get; }

    string RenameLabel { get; }

    string CopyLabel { get; }

    string CutLabel { get; }

    string PasteLabel { get; }

    string DeleteLabel { get; }

    string CopyNameLabel { get; }

    string PasteNameLabel { get; }

    /// <summary>Called whenever the tree structure or a node name changes.</summary>
    void OnTreeChanged();
}
