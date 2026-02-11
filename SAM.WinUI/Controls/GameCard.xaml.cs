using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace SAM.WinUI.Controls;

public sealed partial class GameCard : UserControl
{
    public static readonly DependencyProperty GameNameProperty =
        DependencyProperty.Register(nameof(GameName), typeof(string), typeof(GameCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl), typeof(string), typeof(GameCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AchievementCountProperty =
        DependencyProperty.Register(nameof(AchievementCount), typeof(int), typeof(GameCard), new PropertyMetadata(0));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(GameCard), new PropertyMetadata(false, OnIsSelectedChanged));

    public static readonly DependencyProperty HasDrmProtectionProperty =
        DependencyProperty.Register(nameof(HasDrmProtection), typeof(bool), typeof(GameCard), new PropertyMetadata(false, OnDrmProtectionChanged));

    public static readonly DependencyProperty DrmProtectionInfoProperty =
        DependencyProperty.Register(nameof(DrmProtectionInfo), typeof(string), typeof(GameCard), new PropertyMetadata(string.Empty));

    public string GameName
    {
        get => (string)GetValue(GameNameProperty);
        set => SetValue(GameNameProperty, value);
    }

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public int AchievementCount
    {
        get => (int)GetValue(AchievementCountProperty);
        set => SetValue(AchievementCountProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool HasDrmProtection
    {
        get => (bool)GetValue(HasDrmProtectionProperty);
        set => SetValue(HasDrmProtectionProperty, value);
    }

    public string DrmProtectionInfo
    {
        get => (string)GetValue(DrmProtectionInfoProperty);
        set => SetValue(DrmProtectionInfoProperty, value);
    }

    public GameCard()
    {
        InitializeComponent();
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GameCard card)
        {
            card.SelectionBorder.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnDrmProtectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GameCard card)
        {
            var hasDrm = (bool)e.NewValue;
            card.DrmWarningIcon.Visibility = hasDrm ? Visibility.Visible : Visibility.Collapsed;
            card.DrmOverlay.Visibility = hasDrm ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        SelectionBorder.Opacity = 0.9;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        SelectionBorder.Opacity = 0;
    }
}
