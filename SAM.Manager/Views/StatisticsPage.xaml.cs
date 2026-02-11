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
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using SAM.Core.Models;
using SAM.Core.Services;
using SAM.Core.Utilities;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SAM.Manager.Views;

/// <summary>
/// Page for viewing and editing game statistics.
/// </summary>
public sealed partial class StatisticsPage : Page
{
    private const int MaxHistoryPoints = 30;

    private readonly IAchievementService _achievementService;
    private readonly ISteamService _steamService;
    private readonly IUserDataService _userDataService;
    private readonly ObservableCollection<StatModel> _stats = new();
    private readonly ObservableCollection<string> _availableUsers = new();
    private readonly JsonSerializerOptions _historyJsonOptions = new() { WriteIndented = true };

    private bool _isReadOnly;
    private bool _isInitialized;
    private string? _currentUserId;
    private string? _selectedUserId;
    private ulong _gameId;
    private string _gameName = string.Empty;
    private IEnumerable<StatModel>? _currentStatsCache;
    private StatsHistoryData? _historyData;

    public StatisticsPage()
    {
        try
        {
            Log.Debug("StatisticsPage: Constructor starting");
            InitializeComponent();
            Log.Debug("StatisticsPage: InitializeComponent completed");
            
            _achievementService = App.GetService<IAchievementService>();
            _steamService = App.GetService<ISteamService>();
            _userDataService = App.GetService<IUserDataService>();

            // Wire up ListView ItemsSource immediately after InitializeComponent
            // so ObservableCollection changes are reflected in the UI
            StatsListView.ItemsSource = _stats;
            AccountComboBox.ItemsSource = _availableUsers;
            Log.Debug("StatisticsPage: Constructor completed");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "StatisticsPage constructor failed");
            throw;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        try
        {
            Log.Debug("StatisticsPage.OnNavigatedTo: Starting");
            base.OnNavigatedTo(e);

            if (e.Parameter is not StatisticsPageParameter parameter)
            {
                Log.Debug("StatisticsPage.OnNavigatedTo: No valid parameter, returning");
                return;
            }

            _gameId = parameter.GameId;
            _gameName = parameter.GameName;
            _currentStatsCache = parameter.Stats;

            if (_gameId == 0)
            {
                Log.Debug("StatisticsPage.OnNavigatedTo: GameId is 0, returning");
                return;
            }

            // Update header
            GameNameText.Text = $"Statistiken - {_gameName}";
            GameSubtitleText.Text = $"App ID: {_gameId}";

            // Initialize accounts
            InitializeAccounts();

            // Load data
            if (IsCurrentUserSelected())
            {
                LoadStats(_currentStatsCache);
                LoadHistoryForUser(_currentUserId);
                UpdateReadOnlyState(false);
            }
            else
            {
                LoadStatsFromHistory(_selectedUserId);
                UpdateReadOnlyState(true);
            }

            UpdateEmptyState();
            _isInitialized = true;
            Log.Debug("StatisticsPage.OnNavigatedTo: Completed successfully");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "StatisticsPage.OnNavigatedTo failed");
            throw;
        }
    }

    private void LoadStats(IEnumerable<StatModel>? stats)
    {
        _stats.Clear();

        if (stats != null)
        {
            foreach (var stat in stats)
            {
                Log.Debug($"Adding stat: Id={stat.Id}, DisplayName={stat.DisplayName}, Value={stat.Value}");
                _stats.Add(stat);
            }
        }

        UpdateEmptyState();

        if (_stats.Count > 0)
        {
            StatsListView.SelectedItem = _stats[0];
        }

        Log.Debug($"LoadStats completed. Count={_stats.Count}");
    }

    private void UpdateEmptyState()
    {
        var hasStats = _stats.Count > 0;
        EmptyState.Visibility = hasStats ? Visibility.Collapsed : Visibility.Visible;
        StatsListView.Visibility = hasStats ? Visibility.Visible : Visibility.Collapsed;
    }

    private void InitializeAccounts()
    {
        _availableUsers.Clear();

        var steamId = _steamService.SteamId;
        _currentUserId = steamId > 0 ? steamId.ToString() : null;

        foreach (var user in _userDataService.GetAllUsers())
        {
            _availableUsers.Add(user);
        }

        if (!string.IsNullOrEmpty(_currentUserId) && !_availableUsers.Contains(_currentUserId))
        {
            _availableUsers.Insert(0, _currentUserId);
        }

        if (_availableUsers.Count == 0)
        {
            _availableUsers.Add(_currentUserId ?? "(kein account)");
        }

        _selectedUserId = _currentUserId ?? _availableUsers[0];
        AccountComboBox.SelectedItem = _selectedUserId;
    }

    private bool IsCurrentUserSelected()
    {
        return !string.IsNullOrEmpty(_currentUserId)
            && string.Equals(_currentUserId, _selectedUserId, StringComparison.Ordinal);
    }

    private void UpdateReadOnlyState(bool readOnly)
    {
        _isReadOnly = readOnly;
        SaveButton.IsEnabled = !readOnly;
        ResetAllButton.IsEnabled = !readOnly;

        if (readOnly)
        {
            ShowStatus("Nur Lesen: Daten aus lokalem Userdata-Cache", InfoBarSeverity.Informational);
        }
    }

    #region Event Handlers

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var stat in _stats)
        {
            stat.IsModified = false;
        }
        ShowStatus("Statistiken aktualisiert", InfoBarSeverity.Informational);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly)
        {
            ShowStatus("Speichern ist im Nur-Lesen-Modus deaktiviert", InfoBarSeverity.Warning);
            return;
        }

        var modifiedStats = _stats.Where(s => s.IsModified).ToList();
        if (modifiedStats.Count == 0)
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

        AppendHistorySnapshot(_currentUserId, _stats);
        ShowStatus($"{modifiedStats.Count} Statistik(en) gespeichert", InfoBarSeverity.Success);
    }

    private async void ResetAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly)
        {
            ShowStatus("Zurücksetzen ist im Nur-Lesen-Modus deaktiviert", InfoBarSeverity.Warning);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Statistiken zurücksetzen",
            Content = "Möchtest du wirklich alle Statistiken auf 0 zurücksetzen?\n\nDiese Aktion kann nicht rückgängig gemacht werden.",
            PrimaryButtonText = "Zurücksetzen",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
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

    private void AccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        
        _selectedUserId = AccountComboBox.SelectedItem as string;

        if (IsCurrentUserSelected())
        {
            UpdateReadOnlyState(false);
            LoadStats(_currentStatsCache);
            LoadHistoryForUser(_currentUserId);
        }
        else
        {
            LoadStatsFromHistory(_selectedUserId);
            UpdateReadOnlyState(true);
        }
    }

    private void StatsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        
        if (StatsListView.SelectedItem is StatModel stat)
        {
            UpdateHistoryGraph(stat.Id);
        }
        else
        {
            UpdateHistoryGraph(null);
        }
    }

    private void ValueTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is StatModel stat)
        {
            textBox.IsReadOnly = _isReadOnly || stat.IsProtected;
            textBox.TextChanged += ValueTextBox_TextChanged;
        }
    }

    private void ValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isReadOnly && sender is TextBox textBox && textBox.DataContext is StatModel stat)
        {
            stat.IsModified = true;
        }
    }

    private void ResetStatButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly)
        {
            ShowStatus("Zurücksetzen ist im Nur-Lesen-Modus deaktiviert", InfoBarSeverity.Warning);
            return;
        }

        if (sender is Button button && button.DataContext is StatModel stat)
        {
            ResetStatToOriginal(stat);
        }
    }

    private void HistoryCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (StatsListView.SelectedItem is StatModel stat)
        {
            UpdateHistoryGraph(stat.Id);
        }
    }

    #endregion

    #region Helpers

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;
    }

    private void ResetStatToOriginal(StatModel stat)
    {
        switch (stat)
        {
            case IntStatModel intStat:
                intStat.IntValue = intStat.OriginalValue;
                break;
            case FloatStatModel floatStat:
                floatStat.FloatValue = floatStat.OriginalValue;
                break;
        }
    }

    #endregion

    #region History

    private void LoadStatsFromHistory(string? userId)
    {
        _stats.Clear();
        _historyData = LoadHistory(userId);

        if (_historyData?.Series is null)
        {
            UpdateEmptyState();
            return;
        }

        foreach (var series in _historyData.Series)
        {
            var last = series.Points.LastOrDefault();
            if (last is null) continue;

            if (string.Equals(series.ValueType, "float", StringComparison.OrdinalIgnoreCase))
            {
                var value = (float)last.Value;
                _stats.Add(new FloatStatModel
                {
                    Id = series.StatId,
                    DisplayName = series.DisplayName,
                    FloatValue = value,
                    OriginalValue = value,
                    IsIncrementOnly = false,
                    IsProtected = false,
                    Permission = 0
                });
            }
            else
            {
                var value = (int)last.Value;
                _stats.Add(new IntStatModel
                {
                    Id = series.StatId,
                    DisplayName = series.DisplayName,
                    IntValue = value,
                    OriginalValue = value,
                    IsIncrementOnly = false,
                    IsProtected = false,
                    Permission = 0
                });
            }
        }

        UpdateEmptyState();
        UpdateHistoryGraph(_stats.FirstOrDefault()?.Id);
    }

    private void LoadHistoryForUser(string? userId)
    {
        _historyData = LoadHistory(userId);
        UpdateHistoryGraph(_stats.FirstOrDefault()?.Id);
    }

    private StatsHistoryData? LoadHistory(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        try
        {
            var path = GetHistoryFilePath(userId);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StatsHistoryData>(json, _historyJsonOptions);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load stats history: {ex.Message}");
            return null;
        }
    }

    private void AppendHistorySnapshot(string? userId, IEnumerable<StatModel> stats)
    {
        if (string.IsNullOrEmpty(userId)) return;

        var history = LoadHistory(userId) ?? new StatsHistoryData
        {
            GameId = _gameId,
            GameName = _gameName,
            UserId = userId,
            Series = new List<StatsHistorySeries>()
        };

        var now = DateTime.UtcNow;
        foreach (var stat in stats)
        {
            var series = history.Series.FirstOrDefault(s => s.StatId == stat.Id);
            if (series is null)
            {
                series = new StatsHistorySeries
                {
                    StatId = stat.Id,
                    DisplayName = stat.DisplayName,
                    ValueType = stat is FloatStatModel ? "float" : "int",
                    Points = new List<StatsHistoryPoint>()
                };
                history.Series.Add(series);
            }

            var value = stat is FloatStatModel f ? f.FloatValue : ((IntStatModel)stat).IntValue;
            series.Points.Add(new StatsHistoryPoint
            {
                TimestampUtc = now,
                Value = value
            });

            if (series.Points.Count > MaxHistoryPoints)
            {
                series.Points = series.Points
                    .OrderBy(p => p.TimestampUtc)
                    .TakeLast(MaxHistoryPoints)
                    .ToList();
            }
        }

        try
        {
            var json = JsonSerializer.Serialize(history, _historyJsonOptions);
            File.WriteAllText(GetHistoryFilePath(userId), json);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save stats history: {ex.Message}");
        }
    }

    private string GetHistoryFilePath(string userId)
    {
        var gamePath = AppPaths.GetGamePath(userId, (uint)_gameId);
        return System.IO.Path.Combine(gamePath, "stats_history.json");
    }

    private void UpdateHistoryGraph(string? statId)
    {
        HistoryCanvas.Children.Clear();
        HistoryEmptyText.Visibility = Visibility.Visible;
        HistoryTitleText.Text = "Verlauf";

        if (string.IsNullOrEmpty(statId) || _historyData?.Series is null)
        {
            return;
        }

        var series = _historyData.Series.FirstOrDefault(s => s.StatId == statId);
        if (series?.Points is null || series.Points.Count < 2)
        {
            return;
        }

        HistoryEmptyText.Visibility = Visibility.Collapsed;

        var points = series.Points.OrderBy(p => p.TimestampUtc).ToList();
        var width = Math.Max(HistoryCanvas.ActualWidth, 10);
        var height = Math.Max(HistoryCanvas.ActualHeight, 10);

        var min = points.Min(p => p.Value);
        var max = points.Max(p => p.Value);
        if (Math.Abs(max - min) < double.Epsilon)
        {
            max = min + 1;
        }

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(Colors.DeepSkyBlue),
            StrokeThickness = 2
        };

        for (var i = 0; i < points.Count; i++)
        {
            var t = (double)i / (points.Count - 1);
            var x = t * width;
            var y = height - ((points[i].Value - min) / (max - min)) * height;
            polyline.Points.Add(new Windows.Foundation.Point(x, y));
        }

        HistoryCanvas.Children.Add(polyline);
        HistoryTitleText.Text = $"Verlauf: {series.DisplayName}";
    }

    #endregion

    #region History Data Classes

    private class StatsHistoryData
    {
        public ulong GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<StatsHistorySeries> Series { get; set; } = new();
    }

    private class StatsHistorySeries
    {
        public string StatId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ValueType { get; set; } = "int";
        public List<StatsHistoryPoint> Points { get; set; } = new();
    }

    private class StatsHistoryPoint
    {
        public DateTime TimestampUtc { get; set; }
        public double Value { get; set; }
    }

    #endregion
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
