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

using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SAM.Core.Services;
using SAM.WinUI.Services;
using SAM.WinUI.Views;
using Windows.Graphics;
using WinRT.Interop;

namespace SAM.WinUI;

/// <summary>
/// Main application window with navigation.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private AppWindow? _appWindow;

    public MainWindow()
    {
        Log.Method();
        try
        {
            Log.Debug("Calling InitializeComponent()...");
            InitializeComponent();
            Log.Debug("InitializeComponent() completed");

            Log.Debug("Getting INavigationService from DI...");
            _navigationService = App.GetService<INavigationService>();
            ArgumentNullException.ThrowIfNull(_navigationService, nameof(_navigationService));
            Log.Debug($"INavigationService obtained: {_navigationService.GetType().Name}");
            
            Log.Debug("Getting ILocalizationService from DI...");
            _localizationService = App.GetService<ILocalizationService>();
            _localizationService.LanguageChanged += OnLanguageChanged;

            Log.Debug("Getting ISettingsService from DI...");
            _settingsService = App.GetService<ISettingsService>();
            ArgumentNullException.ThrowIfNull(_settingsService, nameof(_settingsService));
            
            Log.Debug("Setting ContentFrame to navigation service...");
            _navigationService.Frame = ContentFrame;
            Log.Debug($"ContentFrame set. Frame is null: {ContentFrame is null}");

            Log.Debug("Configuring window...");
            ConfigureWindow();
            
            Log.Debug("Configuring title bar...");
            ConfigureTitleBar();
            
            Log.Debug("Subscribing to ContentFrame.Navigated event...");
            if (ContentFrame is not null)
            {
                ContentFrame.Navigated += ContentFrame_Navigated;
                ContentFrame.NavigationFailed += ContentFrame_NavigationFailed;
            }
            Log.Debug("Event handlers attached");

            Closed += MainWindow_Closed;
            
            Log.MethodExit("success");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "MainWindow constructor failed");
            throw;
        }
    }

    private void ContentFrame_NavigationFailed(object sender, Microsoft.UI.Xaml.Navigation.NavigationFailedEventArgs e)
    {
        Log.Error($"NAVIGATION FAILED! SourcePageType: {e.SourcePageType?.FullName}, Exception: {e.Exception}");
        e.Handled = true;
    }

    private void ConfigureWindow()
    {
        Log.Method();
        try
        {
            Log.Debug("Getting window handle...");
            var hwnd = WindowNative.GetWindowHandle(this);
            Log.Debug($"Window handle: {hwnd}");
            
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            Log.Debug($"Window ID: {windowId}");
            
            _appWindow = AppWindow.GetFromWindowId(windowId);
            Log.Debug($"AppWindow obtained: {_appWindow is not null}");

            if (_appWindow is not null)
            {
                Log.Debug("Applying saved window placement...");
                ApplyWindowPlacement(_appWindow, windowId, 1280, 800);
                
                Log.Debug("Setting window title...");
                _appWindow.Title = "Steam Achievement Manager";
            }

            Log.Debug("Trying to set Mica backdrop...");
            var micaResult = TrySetMicaBackdrop();
            Log.Debug($"Mica backdrop result: {micaResult}");
            
            Log.MethodExit();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ConfigureWindow failed");
        }
    }

    private void ConfigureTitleBar()
    {
        Log.Method();
        if (_appWindow is null)
        {
            Log.Warn("AppWindow is null, skipping title bar configuration");
            return;
        }

        Log.Debug($"IsCustomizationSupported: {AppWindowTitleBar.IsCustomizationSupported()}");
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = _appWindow.TitleBar;
            Log.Debug("Extending content into title bar...");
            titleBar.ExtendsContentIntoTitleBar = true;

            Log.Debug("Setting title bar button colors...");
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            
            // Set initial foreground colors based on current theme
            var themeService = App.GetService<IThemeService>();
            var isLightTheme = themeService.CurrentTheme == ElementTheme.Light;
            if (themeService.CurrentTheme == ElementTheme.Default)
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                var foreground = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
                isLightTheme = foreground.R < 128;
            }
            
            if (isLightTheme)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 100, 100, 100);
            }
            else
            {
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 160, 160, 160);
            }

            Log.Debug("Setting title bar drag region...");
            SetTitleBar(AppTitleBar);
        }
        Log.MethodExit();
    }

    private bool TrySetMicaBackdrop()
    {
        Log.Method();
        var isSupported = Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported();
        Log.Debug($"MicaController.IsSupported: {isSupported}");
        
        if (isSupported)
        {
            Log.Debug("Creating MicaBackdrop...");
            var micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            SystemBackdrop = micaBackdrop;
            Log.MethodExit("true");
            return true;
        }

        Log.MethodExit("false");
        return false;
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Method();
        try
        {
            Log.Info("NavigationView loaded - setting initial selection");
            
            // Apply localization to navigation items
            ApplyNavLocalization();
            
            Log.Debug($"NavView.MenuItems.Count: {NavView.MenuItems.Count}");
            for (int i = 0; i < NavView.MenuItems.Count; i++)
            {
                var item = NavView.MenuItems[i];
                Log.Debug($"  MenuItems[{i}]: Type={item?.GetType().Name}, Content={(item as NavigationViewItem)?.Content}, Tag={(item as NavigationViewItem)?.Tag}");
            }
            
            Log.Debug($"NavView.FooterMenuItems.Count: {NavView.FooterMenuItems.Count}");
            for (int i = 0; i < NavView.FooterMenuItems.Count; i++)
            {
                var item = NavView.FooterMenuItems[i];
                Log.Debug($"  FooterMenuItems[{i}]: Type={item?.GetType().Name}, Content={(item as NavigationViewItem)?.Content}, Tag={(item as NavigationViewItem)?.Tag}");
            }

            // Setting SelectedItem triggers NavView_SelectionChanged which handles the navigation
            Log.Debug("Setting NavView.SelectedItem to MenuItems[0]...");
            var firstItem = NavView.MenuItems[0];
            Log.Debug($"First item: {(firstItem as NavigationViewItem)?.Content}, Tag: {(firstItem as NavigationViewItem)?.Tag}");
            NavView.SelectedItem = firstItem;
            Log.Debug($"NavView.SelectedItem is now: {(NavView.SelectedItem as NavigationViewItem)?.Content}");
            
            Log.MethodExit("success");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "NavView_Loaded failed");
        }
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

    private void ApplyNavLocalization()
    {
        NavGamesItem.Content = Loc.Get("Nav.Games");
        NavStatisticsItem.Content = Loc.Get("Nav.Statistics");
        NavDiagnosticsItem.Content = Loc.Get("Nav.Diagnostics");
        NavAboutItem.Content = Loc.Get("Nav.About");
        
        // Settings item is built-in, need to update its content
        if (NavView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = Loc.Get("Nav.Settings");
        }
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // Update navigation items when language changes
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyNavLocalization();
        });
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        Log.Method();
        try
        {
            Log.Info("========== SELECTION CHANGED ==========");
            Log.Debug($"args.IsSettingsSelected: {args.IsSettingsSelected}");
            Log.Debug($"args.SelectedItem: {args.SelectedItem?.GetType().Name ?? "null"}");
            Log.Debug($"args.SelectedItemContainer: {args.SelectedItemContainer?.GetType().Name ?? "null"}");
            
            if (args.SelectedItem is NavigationViewItem item)
            {
                Log.Debug($"SelectedItem.Content: {item.Content}");
                Log.Debug($"SelectedItem.Tag: {item.Tag}");
                Log.Debug($"SelectedItem.IsSelected: {item.IsSelected}");
            }

            Type? pageType = null;
            
            if (args.IsSettingsSelected)
            {
                Log.Info("Settings selected - navigating to SettingsPage");
                pageType = typeof(SettingsPage);
            }
            else if (args.SelectedItem is NavigationViewItem navItem)
            {
                var tag = navItem.Tag?.ToString();
                Log.Info($"Navigation item selected - Tag: '\''{tag}'\''");
                
                pageType = tag switch
                {
                    "games" => typeof(GamePickerPage),
                    "statistics" => typeof(StatisticsPage),
                    "about" => typeof(AboutPage),
                    "diagnostics" => typeof(DiagnosticsPage),
                    _ => null
                };
                Log.Debug($"Resolved pageType: {pageType?.Name ?? "null"}");
            }
            else
            {
                Log.Warn($"SelectedItem is not a NavigationViewItem! Type: {args.SelectedItem?.GetType().FullName}");
            }
            
            if (pageType is not null)
            {
                var currentPageType = ContentFrame.Content?.GetType();
                Log.Debug($"Current page type: {currentPageType?.Name ?? "null"}");
                Log.Debug($"Target page type: {pageType.Name}");
                Log.Debug($"Are they the same? {currentPageType == pageType}");
                
                if (currentPageType != pageType)
                {
                    Log.Info($"Navigating from {currentPageType?.Name ?? "null"} to {pageType.Name}...");
                    var result = ContentFrame.Navigate(pageType);
                    Log.Info($"Navigate() result: {result}");
                    Log.Debug($"ContentFrame.Content after Navigate: {ContentFrame.Content?.GetType().Name ?? "null"}");
                }
                else
                {
                    Log.Debug("Already on target page, skipping navigation");
                }
            }
            else
            {
                Log.Warn("pageType is null - no navigation will occur");
            }
            
            Log.Info("========== END SELECTION CHANGED ==========");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "NavView_SelectionChanged failed");
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        Log.Method();
        Log.Debug($"ContentFrame.CanGoBack: {ContentFrame.CanGoBack}");
        
        if (ContentFrame.CanGoBack)
        {
            Log.Debug("Going back...");
            ContentFrame.GoBack();
            Log.Debug($"After GoBack - ContentFrame.Content: {ContentFrame.Content?.GetType().Name}");
        }
        else
        {
            Log.Debug("Cannot go back - no history");
        }
    }

    /// <summary>
    /// Handles mouse thumb button navigation (XButton1 = Back, XButton2 = Forward).
    /// </summary>
    private void RootGrid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;
        
        if (properties.IsXButton1Pressed)
        {
            // XButton1 = Back button on mouse
            if (_navigationService.CanGoBack)
            {
                Log.Debug("Mouse XButton1 pressed - navigating back");
                _navigationService.GoBack();
                e.Handled = true;
            }
        }
        else if (properties.IsXButton2Pressed)
        {
            // XButton2 = Forward button on mouse
            if (_navigationService.CanGoForward)
            {
                Log.Debug("Mouse XButton2 pressed - navigating forward");
                _navigationService.GoForward();
                e.Handled = true;
            }
        }
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        Log.Method();
        Log.Info("========== FRAME NAVIGATED ==========");
        Log.Debug($"e.SourcePageType: {e.SourcePageType?.FullName}");
        Log.Debug($"e.Content: {e.Content?.GetType().Name ?? "null"}");
        Log.Debug($"e.NavigationMode: {e.NavigationMode}");
        Log.Debug($"e.Parameter: {e.Parameter ?? "null"}");
        
        // Update back button visibility based on navigation history
        Log.Debug($"ContentFrame.CanGoBack: {ContentFrame?.CanGoBack}");
        NavView.IsBackEnabled = ContentFrame?.CanGoBack ?? false;
        Log.Debug($"NavView.IsBackEnabled set to: {NavView.IsBackEnabled}");
        
        var pageType = e.SourcePageType;

        if (pageType == typeof(SettingsPage))
        {
            Log.Debug("Page is SettingsPage - setting SelectedItem to SettingsItem");
            NavView.SelectedItem = NavView.SettingsItem;
            Log.Info("========== END FRAME NAVIGATED (Settings) ==========");
            return;
        }

        Log.Debug("Searching for matching menu item...");
        
        // Find matching menu item
        foreach (var menuItem in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            var tag = menuItem.Tag?.ToString();
            Log.Trace($"Checking MenuItems - Tag: '\''{tag}'\'', Content: '\''{menuItem.Content}'\''");
            
            var isMatch = tag switch
            {
                "games" => pageType == typeof(GamePickerPage),
                "about" => pageType == typeof(AboutPage),
                "diagnostics" => pageType == typeof(DiagnosticsPage),
                _ => false
            };

            if (isMatch)
            {
                Log.Info($"Found matching menu item: Tag='\''{tag}'\'', Content='\''{menuItem.Content}'\''");
                Log.Debug($"Current NavView.SelectedItem: {(NavView.SelectedItem as NavigationViewItem)?.Content ?? "null"}");
                NavView.SelectedItem = menuItem;
                Log.Debug($"After setting SelectedItem: {(NavView.SelectedItem as NavigationViewItem)?.Content ?? "null"}");
                Log.Info("========== END FRAME NAVIGATED (Menu match) ==========");
                return;
            }
        }

        Log.Debug("Not found in MenuItems, checking FooterMenuItems...");
        
        // Check footer items
        foreach (var footerItem in NavView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            var tag = footerItem.Tag?.ToString();
            Log.Trace($"Checking FooterMenuItems - Tag: '\''{tag}'\'', Content: '\''{footerItem.Content}'\''");
            
            var isMatch = tag switch
            {
                "games" => pageType == typeof(GamePickerPage),
                "about" => pageType == typeof(AboutPage),
                "diagnostics" => pageType == typeof(DiagnosticsPage),
                _ => false
            };

            if (isMatch)
            {
                Log.Info($"Found matching footer item: Tag='\''{tag}'\'', Content='\''{footerItem.Content}'\''");
                NavView.SelectedItem = footerItem;
                Log.Info("========== END FRAME NAVIGATED (Footer match) ==========");
                return;
            }
        }

        Log.Warn($"No matching navigation item found for pageType: {pageType?.Name}");
        Log.Info("========== END FRAME NAVIGATED (No match) ==========");
    }
}
