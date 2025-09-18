using Avalonia;
using Avalonia.Controls.Primitives;

namespace AvaloniaStudy.Controls;

public class LargeLabelControl : TemplatedControl
{
    public static readonly StyledProperty<string> LargeTextProperty = AvaloniaProperty.Register<
        LargeLabelControl,
        string
    >(nameof(LargeText), "Large Text Info");

    public string LargeText
    {
        get => GetValue(LargeTextProperty);
        set => SetValue(LargeTextProperty, value);
    }

    public static readonly StyledProperty<string> SmallTextProperty = AvaloniaProperty.Register<
        LargeLabelControl,
        string
    >(nameof(SmallText), "Small Text Info");

    public string SmallText
    {
        get => GetValue(SmallTextProperty);
        set => SetValue(SmallTextProperty, value);
    }
}
