using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SAM.WinUI.Controls;

public sealed partial class AchievementCard : UserControl
{
    public static new readonly DependencyProperty NameProperty =
        DependencyProperty.Register(nameof(Name), typeof(string), typeof(AchievementCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(AchievementCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconUrlProperty =
        DependencyProperty.Register(nameof(IconUrl), typeof(string), typeof(AchievementCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsUnlockedProperty =
        DependencyProperty.Register(nameof(IsUnlocked), typeof(bool), typeof(AchievementCard), new PropertyMetadata(false));

    public static readonly DependencyProperty IsModifiedProperty =
        DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(AchievementCard), new PropertyMetadata(false));

    public static readonly DependencyProperty UnlockDateProperty =
        DependencyProperty.Register(nameof(UnlockDate), typeof(string), typeof(AchievementCard), new PropertyMetadata(string.Empty));

    public new string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string IconUrl
    {
        get => (string)GetValue(IconUrlProperty);
        set => SetValue(IconUrlProperty, value);
    }

    public bool IsUnlocked
    {
        get => (bool)GetValue(IsUnlockedProperty);
        set => SetValue(IsUnlockedProperty, value);
    }

    public bool IsModified
    {
        get => (bool)GetValue(IsModifiedProperty);
        set => SetValue(IsModifiedProperty, value);
    }

    public string UnlockDate
    {
        get => (string)GetValue(UnlockDateProperty);
        set => SetValue(UnlockDateProperty, value);
    }

    public event EventHandler<bool>? UnlockStateChanged;

    public AchievementCard()
    {
        InitializeComponent();
    }

    private void UnlockCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UnlockStateChanged?.Invoke(this, IsUnlocked);
    }
}
