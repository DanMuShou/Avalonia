using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaStudy.ViewModels;

namespace AvaloniaStudy.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var position = ChannelConfigBut.TranslatePoint(new Point(), MainGrid)!.Value;
        ChannelConfigPopupBorder.Margin = new Thickness(
            position.X,
            0,
            0,
            MainGrid.Bounds.Height - position.Y - ChannelConfigBut.Bounds.Height
        );
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) =>
        ((MainViewModel)DataContext).ChannelConfigButPressedCommand.Execute(null);
}
