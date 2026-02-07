using System.Windows;
using System.Windows.Controls;
using Kavopici.Models.Enums;

namespace Kavopici.Components;

public partial class BlendCard : UserControl
{
    public static readonly DependencyProperty BlendNameProperty =
        DependencyProperty.Register(nameof(BlendName), typeof(string), typeof(BlendCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RoasterProperty =
        DependencyProperty.Register(nameof(Roaster), typeof(string), typeof(BlendCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty OriginProperty =
        DependencyProperty.Register(nameof(Origin), typeof(string), typeof(BlendCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty RoastLevelProperty =
        DependencyProperty.Register(nameof(RoastLevel), typeof(RoastLevel), typeof(BlendCard),
            new PropertyMetadata(RoastLevel.Medium));

    public static readonly DependencyProperty SupplierNameProperty =
        DependencyProperty.Register(nameof(SupplierName), typeof(string), typeof(BlendCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CommentProperty =
        DependencyProperty.Register(nameof(Comment), typeof(string), typeof(BlendCard),
            new PropertyMetadata(null));

    public string BlendName
    {
        get => (string)GetValue(BlendNameProperty);
        set => SetValue(BlendNameProperty, value);
    }

    public string Roaster
    {
        get => (string)GetValue(RoasterProperty);
        set => SetValue(RoasterProperty, value);
    }

    public string? Origin
    {
        get => (string?)GetValue(OriginProperty);
        set => SetValue(OriginProperty, value);
    }

    public RoastLevel RoastLevel
    {
        get => (RoastLevel)GetValue(RoastLevelProperty);
        set => SetValue(RoastLevelProperty, value);
    }

    public string SupplierName
    {
        get => (string)GetValue(SupplierNameProperty);
        set => SetValue(SupplierNameProperty, value);
    }

    public string? Comment
    {
        get => (string?)GetValue(CommentProperty);
        set => SetValue(CommentProperty, value);
    }

    public BlendCard()
    {
        InitializeComponent();
    }
}
