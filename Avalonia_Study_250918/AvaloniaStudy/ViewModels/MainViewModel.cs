using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaStudy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _boldTitle = "Avalonia";

    [ObservableProperty]
    private string _regularTitle = "LOUDNESS METER";

    [ObservableProperty]
    private bool _isChannelConfigOpen = false;

    [RelayCommand]
    private void ChannelConfigButPressed() => IsChannelConfigOpen ^= true;
}
