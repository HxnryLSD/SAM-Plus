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

using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SAM.Core.Services;
using SAM.Core.Utilities;

namespace SAM.WinUI.Views;

/// <summary>
/// Read-only statistics overview sourced from local user data.
/// </summary>
public sealed partial class StatisticsPage : Page
{
    private const string AllAccountsLabel = "(Alle)";
    private readonly ObservableCollection<StatEntry> _entries = new();
    private readonly ObservableCollection<string> _accounts = new();
    private readonly IUserDataService _userDataService;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StatisticsPage()
    {
        InitializeComponent();
        _userDataService = App.GetService<IUserDataService>();

        StatsListView.ItemsSource = _entries;
        AccountComboBox.ItemsSource = _accounts;
        Loaded += StatisticsPage_Loaded;
    }

    private void StatisticsPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadAccounts();
        LoadEntries(AccountComboBox.SelectedItem as string);
    }

    private void AccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadEntries(AccountComboBox.SelectedItem as string);
    }

    private void LoadAccounts()
    {
        _accounts.Clear();
        _accounts.Add(AllAccountsLabel);

        foreach (var user in _userDataService.GetAllUsers())
        {
            _accounts.Add(user);
        }

        AccountComboBox.SelectedItem = _accounts[0];
    }

    private void LoadEntries(string? selectedAccount)
    {
        _entries.Clear();

        var accounts = selectedAccount == AllAccountsLabel
            ? _accounts.Where(a => a != AllAccountsLabel)
            : selectedAccount is null
                ? Enumerable.Empty<string>()
                : new[] { selectedAccount };

        foreach (var account in accounts)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                continue;
            }

            foreach (var gameId in AppPaths.GetUserGames(account))
            {
                if (!uint.TryParse(gameId, out var parsedGameId))
                {
                    continue;
                }

                var historyPath = System.IO.Path.Combine(AppPaths.GetGamePath(account, parsedGameId), "stats_history.json");
                if (!System.IO.File.Exists(historyPath))
                {
                    continue;
                }

                var history = ReadHistory(historyPath);
                if (history is null)
                {
                    continue;
                }

                foreach (var series in history.Series)
                {
                    var last = series.Points.OrderBy(p => p.TimestampUtc).LastOrDefault();
                    if (last is null)
                    {
                        continue;
                    }

                    _entries.Add(new StatEntry
                    {
                        UserId = account,
                        GameId = history.GameId,
                        GameName = history.GameName,
                        StatId = series.StatId,
                        DisplayName = series.DisplayName,
                        Value = last.Value,
                        UpdatedUtc = last.TimestampUtc
                    });
                }
            }
        }

        var count = _entries.Count;
        SubtitleText.Text = count == 0 ? "Keine Statistiken gefunden" : $"{count} Eintraege";
        StatusInfoBar.IsOpen = count == 0;
        StatusInfoBar.Message = count == 0 ? "Keine Statistiken im lokalen Userdata-Cache gefunden." : string.Empty;
    }

    private static StatsHistoryData? ReadHistory(string path)
    {
        try
        {
            var json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<StatsHistoryData>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}

public class StatEntry
{
    public string UserId { get; set; } = string.Empty;
    public ulong GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string StatId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public string ValueText => Value.ToString("0.###");
    public string UpdatedText => UpdatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

internal class StatsHistoryData
{
    public ulong GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<StatsHistorySeries> Series { get; set; } = new();
}

internal class StatsHistorySeries
{
    public string StatId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ValueType { get; set; } = "int";
    public List<StatsHistoryPoint> Points { get; set; } = new();
}

internal class StatsHistoryPoint
{
    public DateTime TimestampUtc { get; set; }
    public double Value { get; set; }
}
