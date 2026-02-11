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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    private double _previousCompletionPercentage;
    private bool _hasShownConfettiThisSession;

    public AchievementManagerPage()
    {
        Log.Method();
        ViewModel = App.GetService<AchievementManagerViewModel>();
        ArgumentNullException.ThrowIfNull(ViewModel, nameof(ViewModel));
        Log.Debug($"ViewModel obtained: {ViewModel is not null}");
        InitializeComponent();
        
        // Wire up CommandBar buttons (avoiding WinUI 3 x:Bind bug with AppBarButton)
        SetupCommandBarBindings();
        
        // Subscribe to ViewModel property changes for animations
        ViewModel!.PropertyChanged += ViewModel_PropertyChanged;
        
        Log.MethodExit("success");
    }

    private void SetupCommandBarBindings()
    {
        // Wire up commands
        RefreshButton.Command = ViewModel.RefreshCommand;
        SaveButton.Command = ViewModel.StoreStatsCommand;
        UnlockAllButton.Command = ViewModel.UnlockAllCommand;
        LockAllButton.Command = ViewModel.LockAllCommand;
        InvertAllButton.Command = ViewModel.InvertAllCommand;
        StatisticsButton.Click += StatisticsButton_Click;
        ResetAllButton.Command = ViewModel.ResetAllCommand;
        
        // Wire up IsEnabled bindings
        void UpdateButtonStates(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.CanModifyAchievements))
            {
                var canModify = ViewModel.CanModifyAchievements;
                SaveButton.IsEnabled = canModify;
                UnlockAllButton.IsEnabled = canModify;
                LockAllButton.IsEnabled = canModify;
                InvertAllButton.IsEnabled = canModify;
            }
        }
        
        ViewModel.PropertyChanged += UpdateButtonStates;
        
        // Set initial state
        var initialCanModify = ViewModel.CanModifyAchievements;
        SaveButton.IsEnabled = initialCanModify;
        UnlockAllButton.IsEnabled = initialCanModify;
        LockAllButton.IsEnabled = initialCanModify;
        InvertAllButton.IsEnabled = initialCanModify;
    }

    private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.CompletionPercentage):
                    await HandleCompletionPercentageChangedAsync();
                    break;
                case nameof(ViewModel.HasUnsavedChanges):
                    if (ViewModel.HasUnsavedChanges && !ViewModel.IsBusy)
                    {
                        // Show subtle notification for unsaved changes
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ViewModel_PropertyChanged failed");
        }
    }

    private async Task HandleCompletionPercentageChangedAsync()
    {
        var currentPercentage = ViewModel.CompletionPercentage;
        
        // Check if reached 100% from below 100%
        if (currentPercentage >= 100 && _previousCompletionPercentage < 100 && !_hasShownConfettiThisSession)
        {
            _hasShownConfettiThisSession = true;
            await ConfettiOverlay.PlayCelebrationAsync();
        }
        
        _previousCompletionPercentage = currentPercentage;
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
                ErrorInfoBar.Message = "Keine gültige Spiel-ID. SAM.Manager muss mit einer Spiel-ID gestartet werden.";
                ErrorInfoBar.IsOpen = true;
            }
            
            Log.Info("========== END AchievementManagerPage.OnNavigatedTo ==========");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "OnNavigatedTo failed");
            ErrorInfoBar.Message = $"Fehler beim Laden: {ex.Message}";
            ErrorInfoBar.IsOpen = true;
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
                    Title = "Ungespeicherte Änderungen",
                    Content = "Es gibt ungespeicherte Änderungen. Möchtest du sie speichern?",
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
            LoadingOverlay.Visibility = Visibility.Visible;
            ErrorInfoBar.IsOpen = false;

            // Set GameName from App before loading (so it will be saved in userdata)
            if (!string.IsNullOrEmpty(App.GameName))
            {
                ViewModel.GameName = App.GameName;
            }

            Log.Info("Calling ViewModel.InitializeCommand...");
            await ViewModel.InitializeCommand.ExecuteAsync((long)gameId);
            
            Log.Info($"InitializeCommand completed. Achievements count: {ViewModel.Achievements.Count}");
            Log.Debug($"FilteredAchievements count: {ViewModel.FilteredAchievements.Count}");
            Log.Debug($"GameName: {ViewModel.GameName}");
            Log.Debug($"HasError: {ViewModel.HasError}");
            Log.Debug($"ErrorMessage: {ViewModel.ErrorMessage}");
            
            // Initialize previous completion percentage for confetti tracking
            _previousCompletionPercentage = ViewModel.CompletionPercentage;
            _hasShownConfettiThisSession = ViewModel.CompletionPercentage >= 100;
            
            // Check if ViewModel caught an error
            if (ViewModel.HasError)
            {
                Log.Error($"ViewModel reported error: {ViewModel.ErrorMessage}");
                ErrorInfoBar.Message = ViewModel.ErrorMessage ?? "Unbekannter Fehler beim Laden der Achievements";
                ErrorInfoBar.IsOpen = true;
            }
            else
            {
                // Show success notification
                await NotificationBar.ShowSuccessAsync(
                    $"{ViewModel.Achievements.Count} Achievements geladen",
                    $"{ViewModel.UnlockedCount} freigeschaltet ({ViewModel.CompletionPercentage:F0}%)");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "LoadGameAsync failed");
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
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.FilterType = FilterComboBox.SelectedIndex switch
        {
            1 => AchievementFilterType.Unlocked,
            2 => AchievementFilterType.Locked,
            3 => AchievementFilterType.Modified,
            _ => AchievementFilterType.All
        };
    }

    private void StatisticsButton_Click(object sender, RoutedEventArgs e)
    {
        var param = new StatisticsPageParameter
        {
            GameId = (ulong)ViewModel.GameId,
            GameName = ViewModel.GameName,
            Stats = ViewModel.Stats
        };

        Frame.Navigate(typeof(StatisticsPage), param);
    }
}
