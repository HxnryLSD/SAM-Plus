/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace SAM.Manager.Controls;

/// <summary>
/// Notification severity levels for styling.
/// </summary>
public enum NotificationSeverity
{
    Informational,
    Success,
    Warning,
    Error
}

/// <summary>
/// An enhanced notification bar with icons, progress indicator, and animations.
/// </summary>
public sealed partial class NotificationBar : UserControl
{
    private DispatcherTimer? _autoHideTimer;
    private Grid? _rootGrid;
    private TranslateTransform? _slideTransform;
    private Border? _iconBorder;
    private FontIcon? _statusIcon;
    private TextBlock? _titleText;
    private TextBlock? _messageText;
    private ProgressRing? _progressIndicator;
    private Button? _closeButton;
    
    public NotificationBar()
    {
        InitializeComponent();
        Loaded += NotificationBar_Loaded;
    }

    private void NotificationBar_Loaded(object sender, RoutedEventArgs e)
    {
        _rootGrid = FindByTag<Grid>("RootGrid");
        _slideTransform = _rootGrid?.RenderTransform as TranslateTransform;
        _iconBorder = FindByTag<Border>("IconBorder");
        _statusIcon = FindByTag<FontIcon>("StatusIcon");
        _titleText = FindByTag<TextBlock>("TitleText");
        _messageText = FindByTag<TextBlock>("MessageText");
        _progressIndicator = FindByTag<ProgressRing>("ProgressIndicator");
        _closeButton = FindByTag<Button>("CloseButton");
        if (_closeButton is not null)
        {
            _closeButton.Click += async (s, args) => await HideAsync();
        }
    }

    /// <summary>
    /// Shows a notification with the specified parameters.
    /// </summary>
    public async Task ShowAsync(
        string title, 
        string? message = null, 
        NotificationSeverity severity = NotificationSeverity.Informational,
        bool showProgress = false,
        bool isClosable = true,
        TimeSpan? autoHideDuration = null)
    {
        // Stop any existing auto-hide timer
        _autoHideTimer?.Stop();
        
        // Set content
        if (_titleText is not null) _titleText.Text = title;
        
        if (!string.IsNullOrEmpty(message))
        {
            if (_messageText is not null)
            {
                _messageText.Text = message;
                _messageText.Visibility = Visibility.Visible;
            }
        }
        else
        {
            if (_messageText is not null)
                _messageText.Visibility = Visibility.Collapsed;
        }
        
        // Set icon and colors based on severity
        ApplySeverityStyle(severity);
        
        // Show/hide progress and close button
        if (_progressIndicator is not null)
        {
            _progressIndicator.IsActive = showProgress;
            _progressIndicator.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
        }
        if (_closeButton is not null)
            _closeButton.Visibility = isClosable ? Visibility.Visible : Visibility.Collapsed;
        
        // Show with animation
        if (_rootGrid is not null)
            _rootGrid.Visibility = Visibility.Visible;
        await AnimateShowAsync();
        
        // Setup auto-hide if specified
        if (autoHideDuration.HasValue)
        {
            _autoHideTimer = new DispatcherTimer
            {
                Interval = autoHideDuration.Value
            };
            _autoHideTimer.Tick += async (s, e) =>
            {
                _autoHideTimer?.Stop();
                await HideAsync();
            };
            _autoHideTimer.Start();
        }
    }

    /// <summary>
    /// Shows a quick success notification.
    /// </summary>
    public Task ShowSuccessAsync(string title, string? message = null)
        => ShowAsync(title, message, NotificationSeverity.Success, autoHideDuration: TimeSpan.FromSeconds(3));

    /// <summary>
    /// Shows a quick error notification.
    /// </summary>
    public Task ShowErrorAsync(string title, string? message = null)
        => ShowAsync(title, message, NotificationSeverity.Error, isClosable: true);

    /// <summary>
    /// Shows a progress notification.
    /// </summary>
    public Task ShowProgressAsync(string title, string? message = null)
        => ShowAsync(title, message, NotificationSeverity.Informational, showProgress: true, isClosable: false);

    /// <summary>
    /// Hides the notification with animation.
    /// </summary>
    public async Task HideAsync()
    {
        _autoHideTimer?.Stop();
        await AnimateHideAsync();
        if (_rootGrid is not null)
            _rootGrid.Visibility = Visibility.Collapsed;
    }

    private void ApplySeverityStyle(NotificationSeverity severity)
    {
        var (icon, iconColor, bgColor) = severity switch
        {
            NotificationSeverity.Success => ("\uE73E", Color.FromArgb(255, 16, 124, 16), Color.FromArgb(20, 16, 124, 16)),
            NotificationSeverity.Warning => ("\uE7BA", Color.FromArgb(255, 157, 93, 0), Color.FromArgb(20, 157, 93, 0)),
            NotificationSeverity.Error => ("\uEA39", Color.FromArgb(255, 196, 43, 28), Color.FromArgb(20, 196, 43, 28)),
            _ => ("\uE946", Color.FromArgb(255, 0, 120, 212), Color.FromArgb(20, 0, 120, 212))
        };
        
        if (_statusIcon is not null)
        {
            _statusIcon.Glyph = icon;
            _statusIcon.Foreground = new SolidColorBrush(iconColor);
        }
        if (_iconBorder is not null)
            _iconBorder.Background = new SolidColorBrush(bgColor);
        if (_rootGrid is not null)
            _rootGrid.BorderBrush = new SolidColorBrush(Color.FromArgb(60, iconColor.R, iconColor.G, iconColor.B));
    }

    private async Task AnimateShowAsync()
    {
        if (_slideTransform is null || _rootGrid is null)
        {
            await Task.CompletedTask;
            return;
        }
        
        var storyboard = new Storyboard();
        
        // Slide in from top
        var slideAnimation = new DoubleAnimation
        {
            From = -20,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideAnimation, _slideTransform);
        Storyboard.SetTargetProperty(slideAnimation, "Y");
        storyboard.Children.Add(slideAnimation);
        
        // Fade in
        var fadeAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeAnimation, _rootGrid);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
        storyboard.Children.Add(fadeAnimation);
        
        storyboard.Begin();
        await Task.Delay(250);
    }

    private async Task AnimateHideAsync()
    {
        if (_slideTransform is null || _rootGrid is null)
        {
            await Task.CompletedTask;
            return;
        }
        
        var storyboard = new Storyboard();
        
        // Slide out to top
        var slideAnimation = new DoubleAnimation
        {
            To = -20,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(slideAnimation, _slideTransform);
        Storyboard.SetTargetProperty(slideAnimation, "Y");
        storyboard.Children.Add(slideAnimation);
        
        // Fade out
        var fadeAnimation = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fadeAnimation, _rootGrid);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
        storyboard.Children.Add(fadeAnimation);
        
        storyboard.Begin();
        await Task.Delay(200);
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        await HideAsync();
    }

    private T? FindByTag<T>(string tag) where T : DependencyObject
    {
        if (Content is not DependencyObject root)
            return null;
        return FindByTagRecursive<T>(root, tag);
    }

    private static T? FindByTagRecursive<T>(DependencyObject parent, string tag) where T : DependencyObject
    {
        // Check parent itself
        if (parent is FrameworkElement fe && string.Equals(fe.Tag as string, tag, StringComparison.Ordinal) && parent is T match)
            return match;
        
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement childFe && string.Equals(childFe.Tag as string, tag, StringComparison.Ordinal) && child is T typed)
                return typed;
            var result = FindByTagRecursive<T>(child, tag);
            if (result is not null)
                return result;
        }
        return null;
    }
}
