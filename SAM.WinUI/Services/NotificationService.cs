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

using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace SAM.WinUI.Services;

/// <summary>
/// Service implementation for Windows App SDK toast notifications.
/// </summary>
public class NotificationService : INotificationService
{
    private bool _isInitialized;

    public NotificationService()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
            _isInitialized = true;
        }
        catch
        {
            // Notifications may not be available in all environments
            _isInitialized = false;
        }
    }

    public void ShowInfo(string title, string message)
    {
        ShowNotification(title, message, "??");
    }

    public void ShowSuccess(string title, string message)
    {
        ShowNotification(title, message, "?");
    }

    public void ShowWarning(string title, string message)
    {
        ShowNotification(title, message, "??");
    }

    public void ShowError(string title, string message)
    {
        ShowNotification(title, message, "?");
    }

    private void ShowNotification(string title, string message, string icon)
    {
        if (!_isInitialized) return;

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText($"{icon} {title}")
                .AddText(message);

            var notification = builder.BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Silently fail if notifications aren't available
        }
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // Handle notification click - bring app to foreground
        App.Current.MainWindow?.Activate();
    }
}
