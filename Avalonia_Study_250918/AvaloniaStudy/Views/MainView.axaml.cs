using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaStudy.Services;
using AvaloniaStudy.ViewModels;

namespace AvaloniaStudy.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new AudioService());
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        await ((MainViewModel)DataContext).LoadSettingsCommand.ExecuteAsync(null);
        base.OnLoaded(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var position = ChannelConfigBut.TranslatePoint(new Point(), MainGrid)!.Value;
        Dispatcher.UIThread.Post(() =>
        {
            ChannelConfigPopupBorder.Margin = new Thickness(
                position.X,
                0,
                0,
                MainGrid.Bounds.Height - position.Y - ChannelConfigBut.Bounds.Height
            );
        });
    }
}
