using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AvaloniaStudy.Styles;

public class AnimationPopup : ContentControl
{
    #region 导出属性
    private bool _isOpen = true;

    public static readonly DirectProperty<AnimationPopup, bool> IsOpenProperty =
        AvaloniaProperty.RegisterDirect<AnimationPopup, bool>(
            nameof(IsOpen),
            o => o.IsOpen,
            (o, v) => o.IsOpen = v
        );

    public bool IsOpen
    {
        get => _isOpen;
        set => SetAndRaise(IsOpenProperty, ref _isOpen, value);
    }

    private TimeSpan _animationTime = TimeSpan.FromSeconds(3);

    public static readonly DirectProperty<AnimationPopup, TimeSpan> AnimationTimeProperty =
        AvaloniaProperty.RegisterDirect<AnimationPopup, TimeSpan>(
            nameof(AnimationTime),
            o => o.AnimationTime,
            (o, v) => o.AnimationTime = v
        );

    public TimeSpan AnimationTime
    {
        get => _animationTime;
        set => SetAndRaise(AnimationTimeProperty, ref _animationTime, value);
    }

    private float _underlayOpacity = 0.5f;

    public static readonly DirectProperty<AnimationPopup, float> UnderlayOpacityProperty =
        AvaloniaProperty.RegisterDirect<AnimationPopup, float>(
            nameof(UnderlayOpacity),
            o => o.UnderlayOpacity,
            (o, v) => o.UnderlayOpacity = v
        );

    public float UnderlayOpacity
    {
        get => _underlayOpacity;
        set => SetAndRaise(UnderlayOpacityProperty, ref _underlayOpacity, value);
    }
    #endregion

    private readonly Animation _animation;
    private readonly Border _underlayControl;
    private readonly DoubleTransition _opacityTransition;
    private Grid _grid;
    private bool _isLoaded;
    private bool _isAnimating;
    private Size _desiredSize;

    static AnimationPopup()
    {
        IsOpenProperty.Changed.AddClassHandler<AnimationPopup>((x, e) => x.OnIsOpenChanged(e));
        AnimationTimeProperty.Changed.AddClassHandler<AnimationPopup>(
            (x, e) => x.OnAnimationTimeChanged(e)
        );
    }

    public AnimationPopup()
    {
        var easing = new QuadraticEaseInOut();
        _opacityTransition = new DoubleTransition()
        {
            Property = OpacityProperty,
            Easing = easing,
            Duration = AnimationTime,
        };
        _animation = new Animation
        {
            Duration = _animationTime,
            Easing = easing,
            Children =
            {
                new KeyFrame()
                {
                    Setters = { new Setter(WidthProperty, 0.0), new Setter(HeightProperty, 0.0) },
                    Cue = new Cue(0.0),
                },
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter(WidthProperty, 100.0),
                        new Setter(HeightProperty, 100.0),
                    },
                    Cue = new Cue(1.0),
                },
            },
        };
        _underlayControl = new Border
        {
            IsVisible = false,
            Background = Brushes.Black,
            Opacity = UnderlayOpacity,
            Transitions = [_opacityTransition],
        };
        Loaded += (_, _) =>
        {
            _isLoaded = true;
            _desiredSize = DesiredSize;
            _underlayControl.ZIndex = ZIndex - 1;

            if (Parent is Grid grid)
                _grid = grid;
            else
                throw new Exception("Parent is not Grid");

            _underlayControl.PointerPressed += (_, _) =>
            {
                if (_isAnimating)
                    return;
                IsOpen = false;
            };

            if (_grid.RowDefinitions.Count > 0)
                _underlayControl.SetValue(Grid.RowSpanProperty, _grid.RowDefinitions.Count);
            if (_grid.ColumnDefinitions.Count > 0)
                _underlayControl.SetValue(Grid.ColumnSpanProperty, _grid.ColumnDefinitions.Count);
        };
    }

    private void OnIsOpenChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (!_isLoaded)
        {
            if (!IsOpen)
            {
                Width = 0;
                Height = 0;
                _underlayControl.Opacity = 0;
            }
            return;
        }

        if (_isAnimating)
            return;

        if (!IsOpen)
            _desiredSize = DesiredSize;

        if (IsOpen)
        {
            _animation.PlaybackDirection = PlaybackDirection.Normal;
            _animation.Children[1].Setters[0] = new Setter(WidthProperty, _desiredSize.Width);
            _animation.Children[1].Setters[1] = new Setter(HeightProperty, _desiredSize.Height);
        }
        else
        {
            _animation.PlaybackDirection = PlaybackDirection.Reverse;
            _animation.Children[1].Setters[0] = new Setter(WidthProperty, Bounds.Width);
            _animation.Children[1].Setters[1] = new Setter(HeightProperty, Bounds.Height);
        }
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                if (IsOpen)
                {
                    Width = double.NaN;
                    Height = double.NaN;

                    _grid.Children.Insert(0, _underlayControl);
                    _underlayControl.IsVisible = true;
                    _underlayControl.Opacity = UnderlayOpacity;
                }
                else
                {
                    Width = 0;
                    Height = 0;

                    _underlayControl.Opacity = 0;
                }

                Console.WriteLine("Animation start");
                _isAnimating = true;
                await _animation.RunAsync(this);
                _isAnimating = false;
                Console.WriteLine("Animation completed");

                if (!IsOpen)
                {
                    Console.WriteLine("Control close");
                    _underlayControl.IsVisible = false;
                    if (_grid.Children.Contains(_underlayControl))
                        _grid.Children.Remove(_underlayControl);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private void OnAnimationTimeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        _animation.Duration = AnimationTime;
        _opacityTransition.Duration = AnimationTime;
    }
}
