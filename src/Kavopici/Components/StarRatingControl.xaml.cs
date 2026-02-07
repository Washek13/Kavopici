using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Kavopici.Components;

public partial class StarRatingControl : UserControl
{
    public static readonly DependencyProperty RatingProperty =
        DependencyProperty.Register(nameof(Rating), typeof(int), typeof(StarRatingControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRatingChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(StarRatingControl),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public static readonly DependencyProperty StarSizeProperty =
        DependencyProperty.Register(nameof(StarSize), typeof(double), typeof(StarRatingControl),
            new PropertyMetadata(32.0));

    private static readonly SolidColorBrush FilledBrush = new(Color.FromRgb(0xD4, 0xA0, 0x17)); // Amber/Gold
    private static readonly SolidColorBrush EmptyBrush = new(Color.FromRgb(0xA5, 0xAA, 0xAF)); // Silver
    private static readonly SolidColorBrush HoverBrush = new(Color.FromRgb(0xE8, 0xB8, 0x30)); // Lighter gold

    private static readonly Color GlowColor = Color.FromRgb(0xD4, 0xA0, 0x17);

    private Button[] _stars = null!;
    private ScaleTransform[] _scales = null!;
    private int _hoverRating;

    public int Rating
    {
        get => (int)GetValue(RatingProperty);
        set => SetValue(RatingProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public double StarSize
    {
        get => (double)GetValue(StarSizeProperty);
        set => SetValue(StarSizeProperty, value);
    }

    public StarRatingControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _stars = new[] { Star1, Star2, Star3, Star4, Star5 };
        _scales = new[] { Star1Scale, Star2Scale, Star3Scale, Star4Scale, Star5Scale };
        UpdateStarDisplay(Rating);
        UpdateInteractivity();
    }

    private static void OnRatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StarRatingControl control && control._stars != null)
        {
            control.UpdateStarDisplay((int)e.NewValue);
        }
    }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StarRatingControl control)
        {
            control.UpdateInteractivity();
        }
    }

    private void Star_Click(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly) return;
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int starValue))
        {
            Rating = starValue;
            AnimateClick(starValue - 1);
        }
    }

    private void AnimateClick(int starIndex)
    {
        if (_scales == null || starIndex < 0 || starIndex >= _scales.Length) return;

        var scale = _scales[starIndex];
        var anim = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(300)
        };
        anim.KeyFrames.Add(new SplineDoubleKeyFrame(0.9, KeyTime.FromPercent(0.15)));
        anim.KeyFrames.Add(new SplineDoubleKeyFrame(1.15, KeyTime.FromPercent(0.5)));
        anim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0)));

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    private void Star_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (IsReadOnly) return;
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int starValue))
        {
            _hoverRating = starValue;
            UpdateStarDisplay(starValue, isHover: true);
            // Add glow to hovered stars
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].Effect = i < starValue
                    ? new DropShadowEffect { Color = GlowColor, ShadowDepth = 0, BlurRadius = 8, Opacity = 0.7 }
                    : null;
            }
        }
    }

    private void Star_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (IsReadOnly) return;
        _hoverRating = 0;
        UpdateStarDisplay(Rating);
        // Remove glow
        foreach (var star in _stars)
            star.Effect = null;
    }

    private void UpdateStarDisplay(int rating, bool isHover = false)
    {
        if (_stars == null) return;
        for (int i = 0; i < _stars.Length; i++)
        {
            var brush = (i < rating) ? (isHover ? HoverBrush : FilledBrush) : EmptyBrush;
            _stars[i].Foreground = brush;
        }
    }

    private void UpdateInteractivity()
    {
        if (_stars == null) return;
        foreach (var star in _stars)
        {
            star.IsHitTestVisible = !IsReadOnly;
            star.Cursor = IsReadOnly ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand;
        }
    }
}
