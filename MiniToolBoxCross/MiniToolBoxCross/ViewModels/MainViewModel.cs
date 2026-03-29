using CommunityToolkit.Mvvm.ComponentModel;
using MiniToolBoxCross.ViewModels.Pages;
using MiniToolBoxCross.ViewModels.UserControls;

namespace MiniToolBoxCross.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private LogBoxViewModel _logBoxViewModel;

    [ObservableProperty]
    private SocketServerViewModel _socketServerViewModel;

    [ObservableProperty]
    private SocketClientViewModel _socketClientViewModel;

    public MainViewModel(
        LogBoxViewModel logBoxViewModel,
        SocketServerViewModel socketServerViewModel,
        SocketClientViewModel socketClientViewModel
    )
    {
        LogBoxViewModel = logBoxViewModel;
        SocketServerViewModel = socketServerViewModel;
        SocketClientViewModel = socketClientViewModel;
    }
}
