using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kavopici.Components;

public partial class UserInitialsControl : UserControl
{
    private static readonly Color[] AvatarColors =
    {
        Color.FromRgb(0x6F, 0x4E, 0x37), // Coffee brown
        Color.FromRgb(0x4A, 0x2C, 0x2A), // Coffee medium
        Color.FromRgb(0x8B, 0x69, 0x14), // Coffee light
        Color.FromRgb(0x2C, 0x18, 0x10), // Coffee dark
        Color.FromRgb(0xD4, 0xA0, 0x17), // Amber gold
        Color.FromRgb(0x3C, 0x14, 0x14), // Espresso
        Color.FromRgb(0xA5, 0x0F, 0x14), // Error red (used as accent)
        Color.FromRgb(0xEA, 0x5B, 0x0C), // Warning orange
    };

    public static readonly DependencyProperty UserNameProperty =
        DependencyProperty.Register(nameof(UserName), typeof(string), typeof(UserInitialsControl),
            new PropertyMetadata(string.Empty, OnUserNameChanged));

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(UserInitialsControl),
            new PropertyMetadata(32.0, OnSizeChanged));

    public string UserName
    {
        get => (string)GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double HalfSize => Size / 2;

    public UserInitialsControl()
    {
        InitializeComponent();
    }

    private static void OnUserNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UserInitialsControl control)
            control.UpdateDisplay();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UserInitialsControl control)
        {
            control.OnPropertyChanged(new DependencyPropertyChangedEventArgs(
                SizeProperty, e.OldValue, e.NewValue));
            control.UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        var name = UserName ?? "";
        var initials = GetInitials(name);
        InitialsText.Text = initials;
        InitialsText.FontSize = Size * 0.4;

        var colorIndex = Math.Abs(name.GetHashCode()) % AvatarColors.Length;
        CircleBorder.Background = new SolidColorBrush(AvatarColors[colorIndex]);
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
    }
}
