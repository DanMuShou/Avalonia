using CommunityToolkit.Mvvm.ComponentModel;
using MiniToolBoxCross.ViewModels.Pages;

namespace MiniToolBoxCross.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ForwardHostViewModel _forwardHostViewModel;

    [ObservableProperty]
    private ForwardClientViewModel _forwardClientViewModel;

    public MainViewModel(
        ForwardHostViewModel forwardHostViewModel,
        ForwardClientViewModel forwardClientViewModel
    )
    {
        ForwardHostViewModel = forwardHostViewModel;
        ForwardClientViewModel = forwardClientViewModel;
    }
}
