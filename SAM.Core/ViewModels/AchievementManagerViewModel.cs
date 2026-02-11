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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SAM.Core.Models;
using SAM.Core.Services;

namespace SAM.Core.ViewModels;

/// <summary>
/// ViewModel for the achievement manager view.
/// </summary>
public partial class AchievementManagerViewModel : ViewModelBase
{
    private readonly IAchievementService _achievementService;
    private readonly IImageCacheService _imageCacheService;
    private readonly IUserDataService _userDataService;
    private readonly ISteamService _steamService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderImageUrl))]
    private long _gameId;

    /// <summary>
    /// Gets the Steam header/banner image URL for the current game.
    /// </summary>
    public string HeaderImageUrl => GameId > 0 
        ? $"https://steamcdn-a.akamaihd.net/steam/apps/{GameId}/header.jpg" 
        : string.Empty;

    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AchievementModel> _achievements = [];

    [ObservableProperty]
    private ObservableCollection<AchievementModel> _filteredAchievements = [];

    [ObservableProperty]
    private ObservableCollection<StatModel> _statistics = [];

    /// <summary>
    /// Gets the statistics collection (alias for Statistics property).
    /// </summary>
    public IEnumerable<StatModel> Stats => Statistics;

    [ObservableProperty]
    private AchievementModel? _selectedAchievement;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private AchievementFilterType _filterType = AchievementFilterType.All;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private int _unlockedCount;

    [ObservableProperty]
    private double _completionPercentage;

    /// <summary>
    /// Gets the number of protected achievements in this game.
    /// </summary>
    [ObservableProperty]
    private int _protectedCount;

    /// <summary>
    /// Gets whether any achievements are protected.
    /// </summary>
    public bool HasProtectedAchievements => ProtectedCount > 0;

    /// <summary>
    /// Gets whether achievements can be modified (no DRM protection).
    /// </summary>
    public bool CanModifyAchievements => ProtectedCount == 0;

    /// <summary>
    /// Gets the DRM protection warning message.
    /// </summary>
    public string ProtectedWarningMessage => ProtectedCount > 0
        ? $"{ProtectedCount} von {Achievements.Count} Erfolgen sind geschützt und können nicht bearbeitet werden."
        : string.Empty;

    public AchievementManagerViewModel(
        IAchievementService achievementService, 
        IImageCacheService imageCacheService,
        IUserDataService userDataService,
        ISteamService steamService)
    {
        _achievementService = achievementService;
        _imageCacheService = imageCacheService;
        _userDataService = userDataService;
        _steamService = steamService;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnFilterTypeChanged(AchievementFilterType value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private async Task InitializeAsync(long gameId)
    {
        Log.Debug($"InitializeAsync called with gameId={gameId}");
        await ExecuteWithBusyAsync(async (ct) =>
        {
            GameId = gameId;
            Log.Debug("Calling _achievementService.InitializeAsync");
            await _achievementService.InitializeAsync(gameId, ct);
            Log.Debug("Service initialized, calling LoadDataInternalAsync");
            await LoadDataInternalAsync(ct);
            Log.Debug("LoadDataInternalAsync completed");
        });
        Log.Debug($"InitializeAsync completed. Achievements count={Achievements?.Count ?? 0}");
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteWithBusyAsync(async (ct) =>
        {
            await LoadDataInternalAsync(ct);
        });
    }

    private async Task LoadDataInternalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        Log.Debug("LoadDataInternalAsync: Getting achievements...");
        var achievements = await _achievementService.GetAchievementsAsync(cancellationToken);
        Log.Debug($"LoadDataInternalAsync: Got {achievements?.Count() ?? 0} achievements");
        Achievements = new ObservableCollection<AchievementModel>(achievements ?? []);
        
        cancellationToken.ThrowIfCancellationRequested();
        
        Log.Debug("LoadDataInternalAsync: Getting stats...");
        var stats = await _achievementService.GetStatisticsAsync(cancellationToken);
        Log.Debug($"LoadDataInternalAsync: Got {stats?.Count() ?? 0} stats");
        Statistics = new ObservableCollection<StatModel>(stats ?? []);

        UpdateStats();
        ApplyFilter();
        Log.Debug($"LoadDataInternalAsync: Final Achievements.Count={Achievements.Count}");
        
        // Save game user data
        await SaveGameUserDataAsync(cancellationToken);
    }

    private async Task SaveGameUserDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var steamId = _steamService.SteamId;
            if (steamId == 0)
            {
                Log.Warn("Cannot save game user data: Steam ID is 0");
                return;
            }

            // Ensure current user is set
            _userDataService.SetCurrentUser(steamId.ToString());

            var gameData = new GameUserData
            {
                GameId = (uint)GameId,
                GameName = GameName,
                HasDrmProtection = HasProtectedAchievements,
                ProtectedAchievementCount = ProtectedCount,
                TotalAchievementCount = Achievements.Count,
                UnlockedAchievementCount = UnlockedCount,
                CompletionPercentage = CompletionPercentage,
                DrmProtectionInfo = HasProtectedAchievements ? ProtectedWarningMessage : null,
                IsInitialized = true,
                LastUpdated = DateTime.UtcNow
            };

            await _userDataService.SaveGameDataAsync(gameData);
            Log.Info($"Saved game user data for {GameName} (ID: {GameId})");
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save game user data: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await ExecuteWithBusyAsync(async (ct) =>
        {
            await _achievementService.RefreshAsync(ct);
            await LoadDataInternalAsync(ct);
        });
    }

    [RelayCommand]
    private void ToggleAchievement(AchievementModel? achievement)
    {
        if (achievement is null || achievement.IsProtected)
        {
            return;
        }

        var newState = !achievement.IsUnlocked;
        if (_achievementService.SetAchievement(achievement.Id, newState))
        {
            achievement.IsUnlocked = newState;
            achievement.UnlockTime = newState ? DateTime.Now : null;
            achievement.IsModified = true;
            HasUnsavedChanges = true;
            UpdateStats();
        }
    }

    [RelayCommand]
    private void UnlockAll()
    {
        foreach (var achievement in Achievements.Where(a => !a.IsUnlocked && !a.IsProtected))
        {
            if (_achievementService.SetAchievement(achievement.Id, true))
            {
                achievement.IsUnlocked = true;
                achievement.UnlockTime = DateTime.Now;
                achievement.IsModified = true;
            }
        }
        HasUnsavedChanges = true;
        UpdateStats();
    }

    [RelayCommand]
    private void LockAll()
    {
        foreach (var achievement in Achievements.Where(a => a.IsUnlocked && !a.IsProtected))
        {
            if (_achievementService.SetAchievement(achievement.Id, false))
            {
                achievement.IsUnlocked = false;
                achievement.UnlockTime = null;
                achievement.IsModified = true;
            }
        }
        HasUnsavedChanges = true;
        UpdateStats();
    }

    [RelayCommand]
    private void InvertAll()
    {
        foreach (var achievement in Achievements.Where(a => !a.IsProtected))
        {
            var newState = !achievement.IsUnlocked;
            if (_achievementService.SetAchievement(achievement.Id, newState))
            {
                achievement.IsUnlocked = newState;
                achievement.UnlockTime = newState ? DateTime.Now : null;
                achievement.IsModified = true;
            }
        }
        HasUnsavedChanges = true;
        UpdateStats();
    }

    [RelayCommand]
    private async Task StoreStatsAsync()
    {
        await ExecuteWithBusyAsync(async (ct) =>
        {
            if (_achievementService.StoreStats())
            {
                HasUnsavedChanges = false;
                
                // Clear modified flags
                foreach (var achievement in Achievements)
                {
                    achievement.IsModified = false;
                }
                foreach (var stat in Statistics)
                {
                    stat.IsModified = false;
                }
            }
            else
            {
                SetError("Failed to store stats to Steam.");
            }
            await Task.CompletedTask;
        });
    }

    [RelayCommand]
    private async Task ResetAllAsync()
    {
        await ExecuteWithBusyAsync(async (ct) =>
        {
            if (_achievementService.ResetAllStats(true))
            {
                await _achievementService.RefreshAsync(ct);
                await LoadDataInternalAsync(ct);
            }
            else
            {
                SetError("Failed to reset stats.");
            }
        });
    }

    private void ApplyFilter()
    {
        var filtered = Achievements.AsEnumerable();

        // Apply text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(a => 
                a.Name.ToLowerInvariant().Contains(search) ||
                (a.Description?.ToLowerInvariant().Contains(search) ?? false));
        }

        // Apply status filter
        filtered = FilterType switch
        {
            AchievementFilterType.Unlocked => filtered.Where(a => a.IsUnlocked),
            AchievementFilterType.Locked => filtered.Where(a => !a.IsUnlocked),
            AchievementFilterType.Modified => filtered.Where(a => a.IsModified),
            _ => filtered
        };

        FilteredAchievements = new ObservableCollection<AchievementModel>(filtered);
    }

    private void UpdateStats()
    {
        UnlockedCount = Achievements.Count(a => a.IsUnlocked);
        ProtectedCount = Achievements.Count(a => a.IsProtected);
        CompletionPercentage = Achievements.Count > 0 
            ? (double)UnlockedCount / Achievements.Count * 100 
            : 0;
        OnPropertyChanged(nameof(HasProtectedAchievements));
        OnPropertyChanged(nameof(CanModifyAchievements));
        OnPropertyChanged(nameof(ProtectedWarningMessage));
    }
}

public enum AchievementFilterType
{
    All,
    Unlocked,
    Locked,
    Modified
}
