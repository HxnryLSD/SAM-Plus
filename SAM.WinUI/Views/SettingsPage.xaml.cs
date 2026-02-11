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

using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SAM.Core.Services;
using SAM.WinUI.Services;

namespace SAM.WinUI.Views;

/// <summary>
/// Settings page for application configuration.
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly ILibraryFetchService _libraryFetchService;
    private bool _isInitializing = true;
    private CancellationTokenSource? _fetchCts;

    public SettingsPage()
    {
        _themeService = App.GetService<IThemeService>();
        _localizationService = App.GetService<ILocalizationService>();
        _settingsService = App.GetService<ISettingsService>();
        _libraryFetchService = App.GetService<ILibraryFetchService>();
        InitializeComponent();
        
        // Set current theme selection
        ThemeComboBox.SelectedIndex = _themeService.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0 // System
        };
        
        // Initialize language selection
        InitializeLanguageComboBox();
        
        // Load behavior settings
        WarnUnsavedToggle.IsOn = _settingsService.WarnOnUnsavedChanges;
        ShowHiddenToggle.IsOn = _settingsService.ShowHiddenAchievements;
        
        // Show last fetch status
        UpdateFetchStatus();
        
        // Apply localized strings
        ApplyLocalization();
        
        _isInitializing = false;
    }

    private void InitializeLanguageComboBox()
    {
        var languages = _localizationService.AvailableLanguages.ToList();
        var currentLanguage = _localizationService.CurrentLanguage;
        
        foreach (var lang in languages)
        {
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = lang.NativeName, Tag = lang.Code });
        }
        
        // Select current language
        for (int i = 0; i < LanguageComboBox.Items.Count; i++)
        {
            if (LanguageComboBox.Items[i] is ComboBoxItem item && item.Tag as string == currentLanguage)
            {
                LanguageComboBox.SelectedIndex = i;
                break;
            }
        }
    }

    private void ApplyLocalization()
    {
        // Page title
        PageTitleText.Text = Loc.Get("Settings.Title");
        
        // Appearance section
        AppearanceSectionText.Text = Loc.Get("Settings.Appearance");
        
        // Theme setting
        ThemeTitleText.Text = Loc.Get("Settings.AppTheme");
        ThemeDescriptionText.Text = Loc.Get("Settings.AppThemeDescription");
        ThemeSystemItem.Content = Loc.Get("Settings.Theme.System");
        ThemeLightItem.Content = Loc.Get("Settings.Theme.Light");
        ThemeDarkItem.Content = Loc.Get("Settings.Theme.Dark");
        
        // Language setting
        LanguageTitleText.Text = Loc.Get("Settings.Language");
        LanguageDescriptionText.Text = Loc.Get("Settings.LanguageDescription");
        
        // Behavior section
        BehaviorSectionText.Text = Loc.Get("Settings.Behavior");
        WarnUnsavedTitleText.Text = Loc.Get("Settings.WarnUnsaved");
        WarnUnsavedDescriptionText.Text = Loc.Get("Settings.WarnUnsavedDescription");
        ShowHiddenTitleText.Text = Loc.Get("Settings.ShowHidden");
        ShowHiddenDescriptionText.Text = Loc.Get("Settings.ShowHiddenDescription");
        
        // Data section
        DataSectionText.Text = Loc.Get("Settings.Data");
        ForceFetchTitleText.Text = Loc.Get("Settings.ForceFetch");
        ForceFetchDescriptionText.Text = Loc.Get("Settings.ForceFetchDescription");
        ForceFetchButton.Content = Loc.Get("Settings.ForceFetchButton");
        CancelFetchButton.Content = Loc.Get("Settings.Cancel");
    }
    
    private void UpdateFetchStatus()
    {
        if (_libraryFetchService.LastFetchTime.HasValue)
        {
            var lastFetch = _libraryFetchService.LastFetchTime.Value.ToLocalTime();
            var result = _libraryFetchService.LastResult;
            
            if (result != null)
            {
                ForceFetchStatusText.Text = $"Last sync: {lastFetch:g} - {result.SuccessCount} games synced";
                if (result.FailedCount > 0)
                {
                    ForceFetchStatusText.Text += $", {result.FailedCount} failed";
                }
            }
            else
            {
                ForceFetchStatusText.Text = $"Last sync: {lastFetch:g}";
            }
            ForceFetchStatusText.Visibility = Visibility.Visible;
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        
        var theme = ThemeComboBox.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        
        _themeService.SetTheme(theme);
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        
        if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string languageCode)
        {
            _localizationService.SetLanguage(languageCode);
            ApplyLocalization();
        }
    }

    private async void WarnUnsavedToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        
        _settingsService.WarnOnUnsavedChanges = WarnUnsavedToggle.IsOn;
        await _settingsService.SaveAsync();
    }

    private async void ShowHiddenToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        
        _settingsService.ShowHiddenAchievements = ShowHiddenToggle.IsOn;
        await _settingsService.SaveAsync();
    }
    
    private async void ForceFetchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_libraryFetchService.IsFetching)
        {
            return;
        }
        
        try
        {
            // Update UI to show progress
            ForceFetchButton.IsEnabled = false;
            CancelFetchButton.Visibility = Visibility.Visible;
            FetchProgressPanel.Visibility = Visibility.Visible;
            FetchProgressBar.Value = 0;
            FetchProgressText.Text = "Starting...";
            
            _fetchCts = new CancellationTokenSource();
            
            var progress = new Progress<LibraryFetchProgress>(p =>
            {
                FetchProgressBar.Value = p.PercentComplete;
                FetchProgressText.Text = $"{p.CurrentIndex}/{p.TotalGames}: {p.CurrentGameName}";
                
                if (!p.IsSuccess && p.ErrorMessage != null)
                {
                    FetchProgressText.Text += $" (Failed: {p.ErrorMessage})";
                }
            });
            
            var result = await _libraryFetchService.FetchAllGamesAsync(progress, _fetchCts.Token);
            
            // Show result
            if (result.WasCancelled)
            {
                FetchProgressText.Text = "Sync cancelled.";
            }
            else
            {
                FetchProgressText.Text = $"Completed: {result.SuccessCount} synced, {result.FailedCount} failed, {result.SkippedCount} skipped ({result.Duration.TotalSeconds:F1}s)";
            }
            
            UpdateFetchStatus();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ForceFetchButton_Click failed");
            FetchProgressText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            ForceFetchButton.IsEnabled = true;
            CancelFetchButton.Visibility = Visibility.Collapsed;
            _fetchCts?.Dispose();
            _fetchCts = null;
        }
    }
    
    private void CancelFetchButton_Click(object sender, RoutedEventArgs e)
    {
        _fetchCts?.Cancel();
        CancelFetchButton.IsEnabled = false;
        FetchProgressText.Text = "Cancelling...";
    }
}

