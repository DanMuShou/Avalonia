using CommunityToolkit.Mvvm.ComponentModel;

namespace MiniToolBoxCross.ViewModels.UserControls;

public partial class LogBoxViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _logText = string.Empty;

    public LogBoxViewModel() { }
}
