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
using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SAM.Core.Services;
using SAM.Manager.Views;
using Windows.Graphics;
using WinRT.Interop;

namespace SAM.Manager;

/// <summary>
/// Main window for SAM.Manager - hosts the achievement manager for a single game.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private AppWindow? _appWindow;

    public MainWindow()
    {
        Log.Method();
        
        try
        {
            InitializeComponent();

            _settingsService = App.GetService<ISettingsService>();
            ArgumentNullException.ThrowIfNull(_settingsService, nameof(_settingsService));
            
            ConfigureWindow();
            ConfigureTitleBar();
            
            // Subscribe to navigation events
            ContentFrame.Navigated += ContentFrame_Navigated;
            
            // Navigate to achievement manager page immediately
            NavigateToAchievementManager();

            Closed += MainWindow_Closed;
            
            Log.MethodExit("success");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "MainWindow constructor failed");
            throw;
        }
    }

    private void ConfigureWindow()
    {
        Log.Method();
        
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (_appWindow is not null)
            {
                ApplyWindowPlacement(_appWindow, windowId, 1200, 800);
                
                // Update title with game info
                var title = "Steam Achievement Manager";
                if (App.GameId > 0)
                {
                    title = App.GameName is not null 
                        ? $"{App.GameName} - SAM" 
                        : $"Game {App.GameId} - SAM";
                }
                _appWindow.Title = title;
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ConfigureWindow failed");
        }
        
        Log.MethodExit();
    }

    private void ConfigureTitleBar()
    {
        Log.Method();
        
        try
        {
            // Set title bar text
            var title = "Steam Achievement Manager";
            if (App.GameId > 0)
            {
                title = App.GameName is not null 
                    ? $"{App.GameName} - Achievement Manager" 
                    : $"Game {App.GameId} - Achievement Manager";
            }
            AppTitleTextBlock.Text = title;
            
            // Configure custom title bar
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = _appWindow?.TitleBar;
                if (titleBar is not null)
                {
                    titleBar.ExtendsContentIntoTitleBar = true;
                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    
                    // Set the drag region
                    SetTitleBar(AppTitleBar);
                }
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ConfigureTitleBar failed");
        }
        
        Log.MethodExit();
    }

    private void NavigateToAchievementManager()
    {
        Log.Method();
        Log.Info($"Navigating to AchievementManagerPage with GameId: {App.GameId}");
        
        try
        {
            ContentFrame.Navigate(typeof(AchievementManagerPage), App.GameId);
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Failed to navigate to AchievementManagerPage");
        }
        
        Log.MethodExit();
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        Log.Method();
        Log.Debug($"Navigated to: {e.SourcePageType.Name}");
        
        // Show/hide back button based on navigation stack
        BackButton.Visibility = ContentFrame.CanGoBack 
            ? Visibility.Visible 
            : Visibility.Collapsed;
        
        Log.MethodExit();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Method();
        
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
        
        Log.MethodExit();
    }

    private void ApplyWindowPlacement(AppWindow appWindow, WindowId windowId, int defaultWidth, int defaultHeight)
    {
        var size = new SizeInt32(defaultWidth, defaultHeight);

        if (_settingsService.HasWindowPlacement && _settingsService.WindowWidth > 0 && _settingsService.WindowHeight > 0)
        {
            size = new SizeInt32(_settingsService.WindowWidth, _settingsService.WindowHeight);
        }

        appWindow.Resize(size);

        var targetX = _settingsService.WindowX;
        var targetY = _settingsService.WindowY;
        var displayArea = DisplayArea.GetFromPoint(new PointInt32(targetX, targetY), DisplayAreaFallback.Primary)
            ?? DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

        if (displayArea is not null)
        {
            var work = displayArea.WorkArea;
            if (size.Width > work.Width)
            {
                size.Width = work.Width;
            }

            if (size.Height > work.Height)
            {
                size.Height = work.Height;
            }

            appWindow.Resize(size);
            var clampedX = Math.Clamp(targetX, work.X, work.X + work.Width - size.Width);
            var clampedY = Math.Clamp(targetY, work.Y, work.Y + work.Height - size.Height);

            if (!_settingsService.HasWindowPlacement)
            {
                clampedX = work.X + (work.Width - size.Width) / 2;
                clampedY = work.Y + (work.Height - size.Height) / 2;
            }

            appWindow.Move(new PointInt32(clampedX, clampedY));
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_appWindow is null)
        {
            return;
        }

        try
        {
            var size = _appWindow.Size;
            var position = _appWindow.Position;

            _settingsService.WindowWidth = size.Width;
            _settingsService.WindowHeight = size.Height;
            _settingsService.WindowX = position.X;
            _settingsService.WindowY = position.Y;
            _settingsService.HasWindowPlacement = true;

            await _settingsService.SaveAsync();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Failed to save window placement");
        }
    }
}
