using System.Linq;
using System.Threading.Tasks;
using AvaloniaStudy.Models;
using AvaloniaStudy.Services;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaStudy.ViewModels;

public partial class MainViewModel(IAudioService audioService) : ObservableObject
{
    [ObservableProperty]
    private string _boldTitle = "Avalonia";

    [ObservableProperty]
    private string _regularTitle = "LOUDNESS METER";

    [ObservableProperty]
    private bool _isChannelConfigOpen = false;

    [ObservableProperty]
    private ObservableGroupedCollection<string, ChannelConfigItem> _channelConfigItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectChannelConfigText))]
    private ChannelConfigItem? _selectChannelConfig;

    public string SelectChannelConfigText => SelectChannelConfig?.ShortText ?? "频道配置";

    public MainViewModel()
        : this(new AudioService()) { }

    [RelayCommand]
    private void ChannelConfigButPressed() => IsChannelConfigOpen = true;

    [RelayCommand]
    private void OnChannelConfigItemBtnPressed(ChannelConfigItem item)
    {
        if (!IsChannelConfigOpen || item == SelectChannelConfig)
            return;
        SelectChannelConfig = item;
        IsChannelConfigOpen = false;
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        var channelConfigs = await audioService.GetChannelConfigsList();
        ChannelConfigItems = new ObservableGroupedCollection<string, ChannelConfigItem>(
            channelConfigs.GroupBy(item => item.Group)
        );
    }
}
