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
using SAM.Core.Services;
using Windows.UI;

namespace SAM.WinUI.Services;

/// <summary>
/// Service implementation for managing application theme.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private ElementTheme _currentTheme = ElementTheme.Default;

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public ElementTheme CurrentTheme => _currentTheme;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _settingsService.LoadAsync(cancellationToken);
        
        if (!cancellationToken.IsCancellationRequested)
        {
            SetTheme(_settingsService.Theme);
            ApplyAccentColor(_settingsService.UseSystemAccentColor, _settingsService.AccentColor);
        }
    }

    public void SetTheme(ElementTheme theme)
    {
        _currentTheme = theme;
        ApplyTheme(theme);
        
        // Save to settings
        _settingsService.Theme = theme switch
        {
            ElementTheme.Light => "Light",
            ElementTheme.Dark => "Dark",
            _ => "System"
        };
        _ = _settingsService.SaveAsync();
    }

    public void SetTheme(string themeName)
    {
        var theme = themeName?.ToLowerInvariant() switch
        {
            "light" => ElementTheme.Light,
            "dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        
        SetTheme(theme);
    }

    public void ApplyAccentColor(bool useSystemAccentColor, string accentColor)
    {
        var color = useSystemAccentColor
            ? GetSystemAccentColor()
            : TryParseColor(accentColor, out var parsed)
                ? parsed
                : GetSystemAccentColor();

        ApplyAccentResources(color);

        _settingsService.UseSystemAccentColor = useSystemAccentColor;
        _settingsService.AccentColor = useSystemAccentColor ? _settingsService.AccentColor : accentColor;
        _ = _settingsService.SaveAsync();
    }

    private static void ApplyTheme(ElementTheme theme)
    {
        var window = App.Current.MainWindow;
        if (window?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
        
        // Update title bar button colors for the current theme
        UpdateTitleBarColors(theme, window);
    }

    private static void ApplyAccentResources(Color color)
    {
        var resources = Application.Current.Resources;

        resources["SystemAccentColor"] = color;
        resources["SystemAccentColorLight1"] = Blend(color, Colors.White, 0.3);
        resources["SystemAccentColorLight2"] = Blend(color, Colors.White, 0.5);
        resources["SystemAccentColorLight3"] = Blend(color, Colors.White, 0.7);
        resources["SystemAccentColorDark1"] = Blend(color, Colors.Black, 0.2);
        resources["SystemAccentColorDark2"] = Blend(color, Colors.Black, 0.4);
        resources["SystemAccentColorDark3"] = Blend(color, Colors.Black, 0.6);
    }

    private static Color GetSystemAccentColor()
    {
        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    private static bool TryParseColor(string value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim().TrimStart('#');
        if (trimmed.Length == 6 && uint.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            color = Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            return true;
        }

        if (trimmed.Length == 8 && uint.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber, null, out var argb))
        {
            color = Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
            return true;
        }

        return false;
    }

    private static Color Blend(Color color, Color target, double amount)
    {
        static byte Mix(byte from, byte to, double t) => (byte)Math.Clamp(from + (to - from) * t, 0, 255);

        return Color.FromArgb(
            255,
            Mix(color.R, target.R, amount),
            Mix(color.G, target.G, amount),
            Mix(color.B, target.B, amount));
    }
    
    private static void UpdateTitleBarColors(ElementTheme theme, Window? window)
    {
        if (window is null) return;
        
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
        if (appWindow?.TitleBar is null || !Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            return;
        
        var titleBar = appWindow.TitleBar;
        
        // Determine actual theme (Light or Dark)
        bool isLightTheme = theme == ElementTheme.Light;
        if (theme == ElementTheme.Default)
        {
            // Check system theme
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var foreground = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            isLightTheme = foreground.R < 128; // Dark foreground = Light theme
        }
        
        if (isLightTheme)
        {
            // Light theme: dark button foreground colors
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 100, 100, 100);
        }
        else
        {
            // Dark theme: light button foreground colors
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 160, 160, 160);
        }
    }
}
