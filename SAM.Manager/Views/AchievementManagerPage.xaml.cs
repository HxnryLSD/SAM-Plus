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
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SAM.Core.Models;
using SAM.Core.Services;
using SAM.Core.ViewModels;
using SAM.Manager.Controls;

namespace SAM.Manager.Views;

/// <summary>
/// Page for managing achievements for the current game.
/// In SAM.Manager, this is the main page that loads automatically with the game ID from command line.
/// </summary>
public sealed partial class AchievementManagerPage : Page
{
    public AchievementManagerViewModel ViewModel { get; } = null!;
    private Grid? _loadingOverlay;
    private InfoBar? _errorInfoBar;
    private NotificationBar? _notificationBar;
    private ListView? _achievementsListView;

    public AchievementManagerPage()
    {
        Log.Method();
        ViewModel = App.GetService<AchievementManagerViewModel>();
        ArgumentNullException.ThrowIfNull(ViewModel, nameof(ViewModel));
        Log.Debug($"ViewModel obtained: {ViewModel is not null}");
        DataContext = ViewModel;
        InitializeComponent();
        Loaded += AchievementManagerPage_Loaded;

        // Subscribe to ViewModel property changes for animations
        ViewModel!.PropertyChanged += ViewModel_PropertyChanged;

        Log.MethodExit("success");
    }

    private void AchievementManagerPage_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Debug("AchievementManagerPage_Loaded: Finding controls by type...");

        if (Content is not Grid pageGrid)
        {
            Log.Error("Page Content is not a Grid!");
            return;
        }

        Log.Debug($"  Root Grid has {pageGrid.Children.Count} children");

        // Dump all children types for diagnostics
        for (var i = 0; i < pageGrid.Children.Count; i++)
        {
            var c = pageGrid.Children[i];
            var tag = (c is FrameworkElement fe) ? fe.Tag : null;
            Log.Debug($"  Child[{i}]: Type={c.GetType().Name}, Tag={tag ?? "(null)"}, Row={Grid.GetRow(c as FrameworkElement ?? new Grid())}");
        }

        // Find controls by type - each type is unique on this page.
        // Tag-based lookup fails in WinUI3 due to WinRT interop issues with Panel.Children.
        _achievementsListView = FindFirst<ListView>(pageGrid);
        var searchBox = FindFirst<AutoSuggestBox>(pageGrid);
        var filterComboBox = FindFirst<ComboBox>(pageGrid);
        var commandBar = FindFirst<CommandBar>(pageGrid);

        // Multiple Grids exist - find LoadingOverlay by Grid.RowSpan=6 (it spans all rows)
        // and ErrorInfoBar is the only InfoBar without IsClosable="False" (the DRM one has IsClosable=False)
        foreach (var child in pageGrid.Children)
        {
            if (child is Grid g && Grid.GetRowSpan(g) == 6)
            {
                _loadingOverlay = g;
            }
            // ErrorInfoBar: severity=Error (the DRM one is severity=Warning)
            if (child is InfoBar ib && ib.Severity == InfoBarSeverity.Error)
            {
                _errorInfoBar = ib;
            }
        }

        Log.Debug($"  ListView: {_achievementsListView is not null}");
        Log.Debug($"  SearchBox: {searchBox is not null}");
        Log.Debug($"  FilterComboBox: {filterComboBox is not null}");
        Log.Debug($"  CommandBar: {commandBar is not null}");
        Log.Debug($"  LoadingOverlay: {_loadingOverlay is not null}");
        Log.Debug($"  ErrorInfoBar: {_errorInfoBar is not null}");

        // Create NotificationBar in code to avoid WinRT Connect cast issues
        _notificationBar = new NotificationBar
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetRow(_notificationBar, 4);
        pageGrid.Children.Add(_notificationBar);

        // Hook up event handlers
        if (_achievementsListView is not null)
        {
            _achievementsListView.ContainerContentChanging += AchievementsListView_ContainerContentChanging;
            _achievementsListView.ItemsSource = ViewModel.FilteredAchievements;
            Log.Debug($"  ListView ItemsSource set: {ViewModel.FilteredAchievements?.Count ?? 0} items");
        }

        if (searchBox is not null)
        {
            searchBox.TextChanged += SearchBox_TextChanged;
        }

        if (filterComboBox is not null)
        {
            filterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
        }

        // Find StatisticsButton inside CommandBar.PrimaryCommands
        if (commandBar is not null)
        {
            foreach (var cmd in commandBar.PrimaryCommands)
            {
                if (cmd is AppBarButton btn && string.Equals(btn.Tag as string, "StatisticsButton", StringComparison.Ordinal))
                {
                    btn.Click += StatisticsButton_Click;
                    Log.Debug("  StatisticsButton Click handler attached");
                    break;
                }
            }

            // If Tag lookup didn't work, find by label
            // The Statistics button has Label="Statistiken"
            foreach (var cmd in commandBar.PrimaryCommands)
            {
                if (cmd is AppBarButton btn && btn.Label == "Statistiken")
                {
                    // Only attach if not already attached (from Tag lookup above)
                    btn.Click -= StatisticsButton_Click;
                    btn.Click += StatisticsButton_Click;
                    Log.Debug("  StatisticsButton found by Label and Click handler attached");
                    break;
                }
            }
        }

        Log.Debug("AchievementManagerPage_Loaded: Complete");
    }

    private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.FilteredAchievements):
                    // Update ListView ItemsSource when the collection is replaced
                    if (_achievementsListView is not null)
                    {
                        _achievementsListView.ItemsSource = ViewModel.FilteredAchievements;
                    }
                    break;
                case nameof(ViewModel.HasUnsavedChanges):
                    if (ViewModel.HasUnsavedChanges && !ViewModel.IsBusy)
                    {
                        // Intentionally no-op. Hook for future UI hint.
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ViewModel_PropertyChanged failed");
        }
    }


    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        try
        {
            Log.Method();
            Log.Info("========== AchievementManagerPage.OnNavigatedTo ==========");
            Log.Debug($"e.Parameter: {e.Parameter}");
            Log.Debug($"e.Parameter type: {e.Parameter?.GetType().FullName}");
            Log.Debug($"App.GameId: {App.GameId}");

            base.OnNavigatedTo(e);

            // In SAM.Manager, the game ID comes from the App, not navigation parameter
            uint gameId = App.GameId;

            // Support navigation parameter override for consistency
            if (e.Parameter is uint paramGameId && paramGameId > 0)
            {
                gameId = paramGameId;
                Log.Info($"Using parameter GameId: {gameId}");
            }
            else if (e.Parameter is int paramGameIdInt && paramGameIdInt > 0)
            {
                gameId = (uint)paramGameIdInt;
                Log.Info($"Using parameter GameId (int): {gameId}");
            }
            else if (e.Parameter is long paramGameIdLong && paramGameIdLong > 0)
            {
                gameId = (uint)paramGameIdLong;
                Log.Info($"Using parameter GameId (long): {gameId}");
            }
            else
            {
                Log.Info($"Using App.GameId: {gameId}");
            }

            if (gameId > 0)
            {
                await LoadGameAsync(gameId);
            }
            else
            {
                Log.Error("No valid GameId available!");
                if (_errorInfoBar is not null)
                {
                    _errorInfoBar.Message = "Keine gueltige Spiel-ID. SAM.Manager muss mit einer Spiel-ID gestartet werden.";
                    _errorInfoBar.IsOpen = true;
                }
            }

            Log.Info("========== END AchievementManagerPage.OnNavigatedTo ==========");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "OnNavigatedTo failed");
            if (_errorInfoBar is not null)
            {
                _errorInfoBar.Message = $"Fehler beim Laden: {ex.Message}";
                _errorInfoBar.IsOpen = true;
            }
        }
    }

    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        try
        {
            base.OnNavigatingFrom(e);

            // Warn about unsaved changes
            if (ViewModel.HasUnsavedChanges)
            {
                e.Cancel = true;

                var dialog = new ContentDialog
                {
                    Title = "Ungespeicherte Aenderungen",
                    Content = "Es gibt ungespeicherte Aenderungen. Moechtest du sie speichern?",
                    PrimaryButtonText = "Speichern",
                    SecondaryButtonText = "Verwerfen",
                    CloseButtonText = "Abbrechen",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                var result = await dialog.ShowAsync();

                switch (result)
                {
                    case ContentDialogResult.Primary:
                        await ViewModel.StoreStatsCommand.ExecuteAsync(null);
                        Frame.GoBack();
                        break;
                    case ContentDialogResult.Secondary:
                        Frame.GoBack();
                        break;
                    // Cancel - do nothing, stay on page
                }
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "OnNavigatingFrom failed");
        }
    }

    private async Task LoadGameAsync(uint gameId)
    {
        Log.Method();
        Log.Info($"Loading game with ID: {gameId}");

        try
        {
            Log.Debug("Setting LoadingOverlay visible...");
            if (_loadingOverlay is not null)
            {
                _loadingOverlay.Visibility = Visibility.Visible;
            }

            if (_errorInfoBar is not null)
            {
                _errorInfoBar.IsOpen = false;
            }

            // Set GameName from App before loading (so it will be saved in userdata)
            if (!string.IsNullOrEmpty(App.GameName))
            {
                ViewModel.GameName = App.GameName;
            }

            Log.Info("Calling ViewModel.InitializeCommand...");
            await ViewModel.InitializeCommand.ExecuteAsync((long)gameId);

            Log.Info($"InitializeCommand completed. Achievements count: {ViewModel.Achievements.Count}");
            Log.Debug($"FilteredAchievements total count: {ViewModel.FilteredTotalCount}");
            Log.Debug($"GameName: {ViewModel.GameName}");
            Log.Debug($"HasError: {ViewModel.HasError}");
            Log.Debug($"ErrorMessage: {ViewModel.ErrorMessage}");

            // Check if ViewModel caught an error
            if (ViewModel.HasError)
            {
                if (_errorInfoBar is not null)
                {
                    _errorInfoBar.Message = ViewModel.ErrorMessage ?? "Unbekannter Fehler beim Laden der Achievements";
                    _errorInfoBar.IsOpen = true;
                }
            }
            else
            {
                // Intentionally no success notification.
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "LoadGameAsync failed");
            if (_errorInfoBar is not null)
            {
                _errorInfoBar.Message = ex.Message;
                _errorInfoBar.IsOpen = true;
            }
        }
        finally
        {
            Log.Debug("Hiding LoadingOverlay...");
            if (_loadingOverlay is not null)
            {
                _loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        Log.MethodExit();
    }

    private void AchievementsListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue)
        {
            if (args.ItemContainer.ContentTemplateRoot is FrameworkElement recycledRoot)
            {
                var recycledImage = recycledRoot.FindName("AchievementIconImage") as Image;
                var recycledPlaceholder = recycledRoot.FindName("AchievementIconPlaceholder") as FrameworkElement;
                if (recycledImage is not null)
                {
                    recycledImage.Source = null;
                    recycledImage.Opacity = 0;
                }

                if (recycledPlaceholder is not null)
                {
                    recycledPlaceholder.Visibility = Visibility.Visible;
                }
            }

            return;
        }

        if (args.ItemContainer.ContentTemplateRoot is not FrameworkElement root || args.Item is not AchievementModel achievement)
        {
            return;
        }

        var image = root.FindName("AchievementIconImage") as Image;
        var placeholder = root.FindName("AchievementIconPlaceholder") as FrameworkElement;
        if (image is null)
        {
            return;
        }

        image.Source = null;
        image.Opacity = 0;
        if (placeholder is not null)
        {
            placeholder.Visibility = Visibility.Visible;
        }

        if (string.IsNullOrWhiteSpace(achievement.CurrentIconUrl))
        {
            return;
        }

        var bitmap = new BitmapImage(new Uri(achievement.CurrentIconUrl))
        {
            DecodePixelWidth = 64
        };

        // Hook ImageOpened on the BitmapImage to fade in (can't use XAML event due to Connect cast bug)
        var capturedImage = image;
        var capturedPlaceholder = placeholder;
        bitmap.ImageOpened += (s, ev) =>
        {
            capturedImage.Opacity = 1;
            if (capturedPlaceholder is not null)
            {
                capturedPlaceholder.Visibility = Visibility.Collapsed;
            }
        };

        image.Source = bitmap;
    }

    private void AchievementIcon_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not Image image)
        {
            return;
        }

        image.Opacity = 1;
        if (image.Parent is FrameworkElement parent)
        {
            var placeholder = parent.FindName("AchievementIconPlaceholder") as FrameworkElement;
            if (placeholder is not null)
            {
                placeholder.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        ViewModel.FilterType = comboBox.SelectedIndex switch
        {
            1 => AchievementFilterType.Unlocked,
            2 => AchievementFilterType.Locked,
            3 => AchievementFilterType.Modified,
            _ => AchievementFilterType.All
        };
    }

    private void StatisticsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("StatisticsButton_Click: Creating navigation parameter");
            var param = new StatisticsPageParameter
            {
                GameId = (ulong)ViewModel.GameId,
                GameName = ViewModel.GameName,
                Stats = ViewModel.Stats
            };

            Log.Debug($"StatisticsButton_Click: Navigating with GameId={param.GameId}, GameName={param.GameName}, Stats count={param.Stats?.Count() ?? 0}");
            Frame.Navigate(typeof(StatisticsPage), param);
            Log.Debug("StatisticsButton_Click: Navigation completed");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "StatisticsButton_Click failed");
            throw;
        }
    }

    /// <summary>
    /// Finds the first element of the given type in a Panel's Children, recursing into nested Panels.
    /// </summary>
    private static T? FindFirst<T>(Panel parent) where T : UIElement
    {
        foreach (var child in parent.Children)
        {
            if (child is T match)
            {
                return match;
            }

            if (child is Panel childPanel)
            {
                var result = FindFirst<T>(childPanel);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        return null;
    }
}
