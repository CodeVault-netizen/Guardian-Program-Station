using System.Windows.Input;

namespace Guardian.ProgramStation.UI.ViewModels;

public interface ITreeNodeHost
{
    ICommand RemoveNodeCommand { get; }

    string AddLabel { get; }

    string RemoveLabel { get; }
}
