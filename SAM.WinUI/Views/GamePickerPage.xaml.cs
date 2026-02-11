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

using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SAM.Core.Models;
using SAM.Core.Services;
using SAM.Core.ViewModels;

namespace SAM.WinUI.Views;

/// <summary>
/// Page for selecting a game to manage achievements.
/// </summary>
public sealed partial class GamePickerPage : Page
{
    public GamePickerViewModel ViewModel { get; } = null!;
    private readonly ISettingsService _settingsService;
    
    // Prevent double-click launching multiple instances
    private bool _isLaunching;

    public GamePickerPage()
    {
        Log.Method();
        try
        {
            Log.Debug("Getting GamePickerViewModel from DI...");
            ViewModel = App.GetService<GamePickerViewModel>();
            _settingsService = App.GetService<ISettingsService>();
            ArgumentNullException.ThrowIfNull(ViewModel, nameof(ViewModel));
            Log.Debug($"ViewModel obtained: {ViewModel is not null}");
            
            Log.Debug("Calling InitializeComponent()...");
            InitializeComponent();
            Log.Debug("InitializeComponent() completed");
            
            // Apply localization
            ApplyLocalization();
            
            // Load saved view type
            LoadViewTypeFromSettings();
            
            Log.MethodExit("success");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "GamePickerPage constructor failed");
            throw;
        }
    }

    private void LoadViewTypeFromSettings()
    {
        var viewType = _settingsService.GameViewType;
        Log.Debug($"Loading saved view type: {viewType}");
        
        switch (viewType)
        {
            case 1:
                ViewCompactButton.IsChecked = true;
                break;
            case 2:
                ViewDetailButton.IsChecked = true;
                break;
            default:
                ViewDefaultButton.IsChecked = true;
                break;
        }
        
        ApplyViewType(viewType);
    }

    private void ApplyViewType(int viewType)
    {
        Log.Debug($"Applying view type: {viewType}");
        
        switch (viewType)
        {
            case 1: // Compact List
                GamesRepeaterScroll.Visibility = Visibility.Collapsed;
                GamesListRepeaterScroll.Visibility = Visibility.Visible;
                break;
            case 2: // Detail Cards
                GamesRepeaterScroll.Visibility = Visibility.Visible;
                GamesListRepeaterScroll.Visibility = Visibility.Collapsed;
                GamesRepeater.ItemTemplate = (DataTemplate)Resources["DetailViewTemplate"];
                GamesRepeaterLayout.MinItemWidth = 320;
                GamesRepeaterLayout.MinItemHeight = 180;
                break;
            default: // Default Cards
                GamesRepeaterScroll.Visibility = Visibility.Visible;
                GamesListRepeaterScroll.Visibility = Visibility.Collapsed;
                GamesRepeater.ItemTemplate = (DataTemplate)Resources["DefaultViewTemplate"];
                GamesRepeaterLayout.MinItemWidth = 230;
                GamesRepeaterLayout.MinItemHeight = 120;
                break;
        }
    }

    private async void ViewTypeButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton) return;
        
        int viewType = radioButton.Name switch
        {
            "ViewCompactButton" => 1,
            "ViewDetailButton" => 2,
            _ => 0
        };
        
        Log.Debug($"View type changed to: {viewType}");
        
        ApplyViewType(viewType);
        
        // Save to settings
        _settingsService.GameViewType = viewType;
        await _settingsService.SaveAsync();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = Loc.Get("GamePicker.Title");
        SearchBox.PlaceholderText = Loc.Get("GamePicker.SearchPlaceholder");
        FilterAllItem.Content = Loc.Get("GamePicker.Filter.All");
        FilterGamesItem.Content = Loc.Get("GamePicker.Filter.GamesOnly");
        FilterModsItem.Content = Loc.Get("GamePicker.Filter.ModsOnly");
        FilterDlcsItem.Content = Loc.Get("GamePicker.Filter.DlcsOnly");
        FilterDemosItem.Content = Loc.Get("GamePicker.Filter.DemosOnly");
        RefreshButtonText.Text = Loc.Get("GamePicker.Refresh");
        LoadingText.Text = Loc.Get("GamePicker.InitializingSteam");
        RefreshIndicatorText.Text = Loc.Get("GamePicker.Refreshing");
        ErrorInfoBar.Title = Loc.Get("Common.Error");
        EmptyTitleText.Text = Loc.Get("GamePicker.NoGames");
        EmptyDescriptionText.Text = Loc.Get("GamePicker.EnsureSteamRunning");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        Log.Method();
        Log.Info("========== GamePickerPage.OnNavigatedTo ==========");
        Log.Debug($"NavigationMode: {e.NavigationMode}");
        Log.Debug($"Parameter: {e.Parameter}");
        Log.Debug($"SourcePageType: {e.SourcePageType}");
        
        base.OnNavigatedTo(e);

        try
        {
            Log.Debug($"ViewModel.Games.Count: {ViewModel.Games.Count}");
            if (ViewModel.Games.Count == 0)
            {
                Log.Info("No games loaded yet, calling LoadGamesAsync()...");
                await LoadGamesAsync();
            }
            else
            {
                Log.Debug("Games already loaded, skipping load");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "OnNavigatedTo failed");
        }
        
        Log.Info("========== END GamePickerPage.OnNavigatedTo ==========");
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Log.Method();
        Log.Debug($"Navigating away from GamePickerPage to: {e.SourcePageType}");
        base.OnNavigatedFrom(e);
    }

    private async Task LoadGamesAsync()
    {
        Log.Method();
        try
        {
            Log.Debug("Preparing loading state...");
            LoadingOverlay.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorInfoBar.IsOpen = false;

            var hasCachedGames = await ViewModel.TryLoadCachedGamesAsync();
            if (hasCachedGames)
            {
                UpdateEmptyState();
            }
            else
            {
                Log.Debug("Setting LoadingOverlay visible...");
                LoadingText.Text = Loc.Get("GamePicker.InitializingSteam");
                LoadingOverlay.Visibility = Visibility.Visible;
            }

            Log.Info("Executing ViewModel.LoadGamesCommand...");
            await ViewModel.LoadGamesCommand.ExecuteAsync(null);
            
            Log.Debug($"After LoadGamesCommand - Games.Count: {ViewModel.Games.Count}");
            Log.Debug($"After LoadGamesCommand - FilteredGames.Count: {ViewModel.FilteredGames.Count}");
            Log.Debug($"After LoadGamesCommand - HasError: {ViewModel.HasError}");
            Log.Debug($"After LoadGamesCommand - ErrorMessage: {ViewModel.ErrorMessage}");

            // Check if ViewModel caught an error
            if (ViewModel.HasError)
            {
                Log.Error($"ViewModel reported error: {ViewModel.ErrorMessage}");
                ErrorInfoBar.Message = ViewModel.ErrorMessage ?? "Unbekannter Fehler";
                ErrorInfoBar.IsOpen = true;
            }
            else if (ViewModel.FilteredGames.Count == 0)
            {
                Log.Warn("No filtered games found, showing empty state");
                EmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                Log.Info($"Loaded {ViewModel.FilteredGames.Count} games successfully");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "LoadGamesAsync failed");
            ErrorInfoBar.Message = ex.Message;
            ErrorInfoBar.IsOpen = true;
        }
        finally
        {
            Log.Debug("Hiding LoadingOverlay...");
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
        
        Log.MethodExit();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        Log.Trace($"SearchBox_TextChanged - Reason: {args.Reason}, Text: '\''{sender.Text}'\''");
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
            UpdateEmptyState();
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Skip if called during InitializeComponent (controls not yet initialized)
        if (EmptyState is null || ViewModel is null)
        {
            Log.Debug("FilterComboBox_SelectionChanged - Skipping (controls not initialized)");
            return;
        }
        
        Log.Debug($"FilterComboBox_SelectionChanged - SelectedIndex: {FilterComboBox.SelectedIndex}");
        ViewModel.FilterType = FilterComboBox.SelectedIndex switch
        {
            1 => GameFilterType.GamesOnly,
            2 => GameFilterType.ModsOnly,
            3 => GameFilterType.DlcOnly,
            4 => GameFilterType.DemosOnly,
            _ => GameFilterType.All
        };
        Log.Debug($"FilterType set to: {ViewModel.FilterType}");
        UpdateEmptyState();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Info("RefreshButton clicked");
            await LoadGamesAsync();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "RefreshButton_Click failed");
        }
    }

    private void GameItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Log.Method();
        
        // Prevent double-click
        if (_isLaunching)
        {
            Log.Debug("Already launching SAM.Manager, ignoring click");
            return;
        }
        
        var element = sender as FrameworkElement;
        if (element?.DataContext is GameModel game)
        {
            Log.Info($"Game clicked - Id: {game.Id}, Name: {game.Name}");
            
            // Launch SAM.Manager.exe with the game ID as command-line argument
            // This ensures each game has its own process with the correct SteamAppId
            LaunchSamManager(game);
        }
        else
        {
            Log.Warn($"ClickedItem is not a GameModel! Type: {element?.DataContext?.GetType().FullName}");
        }
    }

    private void GamesRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is not FrameworkElement element)
        {
            return;
        }

        if (sender.ItemsSourceView?.GetAt(args.Index) is not GameModel game)
        {
            return;
        }

        element.DataContext = game;

        var image = element.FindName("GameImage") as Image;
        var placeholder = element.FindName("ImagePlaceholder") as FrameworkElement;
        if (image is null)
        {
            return;
        }

        image.Opacity = 0;
        if (placeholder is not null)
        {
            placeholder.Visibility = Visibility.Visible;
        }

        image.Source = null;
        if (string.IsNullOrWhiteSpace(game.ImageUrl))
        {
            return;
        }

        var decodeWidth = image.Width > 0 ? (int)image.Width : (element.Width > 0 ? (int)element.Width : 256);
        var bitmap = new BitmapImage(new Uri(game.ImageUrl))
        {
            DecodePixelWidth = Math.Max(32, decodeWidth)
        };
        image.Tag = new ImageTag(placeholder);
        image.Source = bitmap;
    }

    private void GamesRepeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        if (args.Element is not FrameworkElement element)
        {
            return;
        }

        element.DataContext = null;

        var image = element.FindName("GameImage") as Image;
        var placeholder = element.FindName("ImagePlaceholder") as FrameworkElement;
        if (image is not null)
        {
            image.Source = null;
            image.Opacity = 0;
        }

        if (placeholder is not null)
        {
            placeholder.Visibility = Visibility.Visible;
        }
    }

    private void GameImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not Image image)
        {
            return;
        }

        image.Opacity = 1;
        if (image.Tag is ImageTag tag && tag.Placeholder is not null)
        {
            tag.Placeholder.Visibility = Visibility.Collapsed;
        }
    }

    private sealed record ImageTag(FrameworkElement? Placeholder);

    private void UpdateEmptyState()
    {
        // Skip if called before controls are initialized
        if (EmptyState is null || ViewModel is null)
        {
            Log.Trace("UpdateEmptyState - Skipping (controls not initialized)");
            return;
        }
        
        var isEmpty = ViewModel.FilteredGames.Count == 0;
        Log.Trace($"UpdateEmptyState - FilteredGames.Count: {ViewModel.FilteredGames.Count}, isEmpty: {isEmpty}");
        EmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Launches SAM.Manager.exe with the specified game ID.
    /// SAM.Manager is a separate process that initializes Steam with the correct AppID.
    /// </summary>
    private async void LaunchSamManager(GameModel game)
    {
        Log.Method();
        
        // Set launching flag to prevent double-clicks
        _isLaunching = true;
        
        Log.Info($"Launching SAM.Manager for game: {game.Id} - {game.Name}");
        
        try
        {
            // SAM.Manager.exe is in a sibling directory (separate output folders)
            var currentDir = AppContext.BaseDirectory;
            var samManagerPath = Path.Combine(currentDir, "..", "SAM.Manager", "SAM.Manager.exe");
            samManagerPath = Path.GetFullPath(samManagerPath);
            
            Log.Debug($"Checking SAM.Manager path: {samManagerPath}");
            
            if (!File.Exists(samManagerPath))
            {
                Log.Error($"SAM.Manager.exe not found at: {samManagerPath}");
                ShowErrorMessage("SAM.Manager.exe nicht gefunden", 
                    $"Die Datei wurde erwartet unter:\n{samManagerPath}\n\nBitte SAM.Manager erst bauen!");
                return;
            }
            
            Log.Debug($"Using SAM.Manager path: {samManagerPath}");
            
            // Launch SAM.Manager with gameId and gameName as arguments
            var startInfo = new ProcessStartInfo
            {
                FileName = samManagerPath,
                Arguments = $"{game.Id} \"{game.Name}\"",
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(samManagerPath)
            };
            
            Log.Info($"Starting process: {startInfo.FileName} {startInfo.Arguments}");
            
            var process = Process.Start(startInfo);
            
            if (process is not null)
            {
                Log.Info($"SAM.Manager started successfully (PID: {process.Id})");
            }
            else
            {
                Log.Error("Process.Start returned null");
                ShowErrorMessage("Fehler beim Starten", "SAM.Manager konnte nicht gestartet werden.");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Failed to launch SAM.Manager");
            ShowErrorMessage("Fehler beim Starten", ex.Message);
        }
        finally
        {
            // Reset launching flag after a delay to allow for accidental double-clicks
            await Task.Delay(1000);
            _isLaunching = false;
        }
        
        Log.MethodExit();
    }

    private async void ShowErrorMessage(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ShowErrorMessage failed");
        }
    }
}
