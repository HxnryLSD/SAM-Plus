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
using System.Collections.ObjectModel;

namespace SAM.Manager.Views;

/// <summary>
/// Page for viewing and editing game statistics.
/// </summary>
public sealed partial class StatisticsPage : Page
{
    private readonly IAchievementService _achievementService;
    private readonly ObservableCollection<StatModel> _stats = new();
    private ulong _gameId;
    private string _gameName = string.Empty;

    public StatisticsPage()
    {
        Log.Method();
        try
        {
            _achievementService = App.GetService<IAchievementService>();
            InitializeComponent();
            StatsListView.ItemsSource = _stats;
            Log.Debug("StatisticsPage initialized");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "StatisticsPage constructor failed");
            throw;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Log.Method();
        Log.Debug($"OnNavigatedTo - Parameter: {e.Parameter}");
        
        try
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is StatisticsPageParameter param)
            {
                Log.Debug($"StatisticsPageParameter received: GameId={param.GameId}, GameName={param.GameName}, Stats count={param.Stats?.Count() ?? -1}");
                
                _gameId = param.GameId;
                _gameName = param.GameName;
                GameNameText.Text = $"Statistiken - {_gameName}";
                GameSubtitleText.Text = $"App ID: {_gameId}";

                // Load stats
                LoadStats(param.Stats);
            }
            else
            {
                Log.Warn($"Parameter is not StatisticsPageParameter: {e.Parameter?.GetType().FullName ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "OnNavigatedTo failed");
            throw;
        }
    }

    private void LoadStats(IEnumerable<StatModel>? stats)
    {
        Log.Method();
        Log.Debug($"LoadStats called with {stats?.Count() ?? -1} stats");
        
        try
        {
            _stats.Clear();

            if (stats != null)
            {
                int i = 0;
                foreach (var stat in stats)
                {
                    Log.Debug($"Adding stat {i++}: Id={stat.Id}, DisplayName={stat.DisplayName}, Value={stat.Value}, Type={stat.GetType().Name}");
                    _stats.Add(stat);
                }
            }

            UpdateEmptyState();
            Log.Debug($"LoadStats completed. _stats.Count={_stats.Count}");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "LoadStats failed");
            throw;
        }
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility = _stats.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        StatsListView.Visibility = _stats.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset modified flags
        foreach (var stat in _stats)
        {
            stat.IsModified = false;
        }

        ShowStatus("Statistiken aktualisiert", InfoBarSeverity.Informational);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var modifiedStats = _stats.Where(s => s.IsModified).ToList();
        var modifiedCount = modifiedStats.Count;

        if (modifiedCount == 0)
        {
            ShowStatus("Keine Änderungen zum Speichern", InfoBarSeverity.Warning);
            return;
        }

        if (!_achievementService.IsReady)
        {
            ShowStatus("Statistiken sind noch nicht geladen", InfoBarSeverity.Error);
            return;
        }

        foreach (var stat in modifiedStats)
        {
            if (!_achievementService.SetStatistic(stat.Id, stat.Value))
            {
                ShowStatus($"Fehler beim Setzen von {stat.Id}. Speichern abgebrochen", InfoBarSeverity.Error);
                return;
            }
        }

        if (!_achievementService.StoreStats())
        {
            ShowStatus("Speichern fehlgeschlagen", InfoBarSeverity.Error);
            return;
        }

        foreach (var stat in modifiedStats)
        {
            stat.AcceptChanges();
        }

        ShowStatus($"{modifiedCount} Statistik(en) gespeichert", InfoBarSeverity.Success);
    }

    private async void ResetAllButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Statistiken zurücksetzen",
                Content = "Möchtest du wirklich alle Statistiken auf 0 zurücksetzen?\n\nDiese Aktion kann nicht rückgängig gemacht werden.",
                PrimaryButtonText = "Zurücksetzen",
                CloseButtonText = "Abbrechen",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                foreach (var stat in _stats)
                {
                    stat.Value = 0;
                    stat.IsModified = true;
                }

                ShowStatus("Alle Statistiken auf 0 gesetzt", InfoBarSeverity.Warning);
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ResetAllButton_Click failed");
            ShowStatus($"Fehler: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;
    }

    private void ValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Mark stat as modified when value changes
        if (sender is TextBox textBox && textBox.DataContext is StatModel stat)
        {
            stat.IsModified = true;
        }
    }
}

/// <summary>
/// Navigation parameter for StatisticsPage.
/// </summary>
public class StatisticsPageParameter
{
    public ulong GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public IEnumerable<StatModel>? Stats { get; set; }
}
