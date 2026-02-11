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
    
    public NotificationBar()
    {
        InitializeComponent();
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
        TitleText.Text = title;
        
        if (!string.IsNullOrEmpty(message))
        {
            MessageText.Text = message;
            MessageText.Visibility = Visibility.Visible;
        }
        else
        {
            MessageText.Visibility = Visibility.Collapsed;
        }
        
        // Set icon and colors based on severity
        ApplySeverityStyle(severity);
        
        // Show/hide progress and close button
        ProgressIndicator.IsActive = showProgress;
        ProgressIndicator.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
        CloseButton.Visibility = isClosable ? Visibility.Visible : Visibility.Collapsed;
        
        // Show with animation
        RootGrid.Visibility = Visibility.Visible;
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
        RootGrid.Visibility = Visibility.Collapsed;
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
        
        StatusIcon.Glyph = icon;
        StatusIcon.Foreground = new SolidColorBrush(iconColor);
        IconBorder.Background = new SolidColorBrush(bgColor);
        RootGrid.BorderBrush = new SolidColorBrush(Color.FromArgb(60, iconColor.R, iconColor.G, iconColor.B));
    }

    private async Task AnimateShowAsync()
    {
        var storyboard = new Storyboard();
        
        // Slide in from top
        var slideAnimation = new DoubleAnimation
        {
            From = -20,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideAnimation, SlideTransform);
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
        Storyboard.SetTarget(fadeAnimation, RootGrid);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
        storyboard.Children.Add(fadeAnimation);
        
        storyboard.Begin();
        await Task.Delay(250);
    }

    private async Task AnimateHideAsync()
    {
        var storyboard = new Storyboard();
        
        // Slide out to top
        var slideAnimation = new DoubleAnimation
        {
            To = -20,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(slideAnimation, SlideTransform);
        Storyboard.SetTargetProperty(slideAnimation, "Y");
        storyboard.Children.Add(slideAnimation);
        
        // Fade out
        var fadeAnimation = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fadeAnimation, RootGrid);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
        storyboard.Children.Add(fadeAnimation);
        
        storyboard.Begin();
        await Task.Delay(200);
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        await HideAsync();
    }
}
