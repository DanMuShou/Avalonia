using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MiniToolBoxCross.Common.Messages.Log;

namespace MiniToolBoxCross.ViewModels.UserControls;

public partial class LogBoxViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _logText = string.Empty;

    public LogBoxViewModel()
    {
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<ConsoleMessage>>(
            this,
            (_, message) => LogText = message.Value.Message + "\n" + LogText
        );
    }
}
