/* Copyright (c) 2024-2026 Rick (rick ''at'' gibbed ''dot'' us)
 *
 * This software is provided ''as-is'', without any express or implied
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

using System.Globalization;
using SAM.API;
using SAM.API.Types;
using SAM.Core.Models;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Achievement service implementation wrapping SAM.API for a specific game.
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly ISteamService _steamService;
    private readonly ISteamCallbackService _callbackService;
    private long _gameId;
    private bool _isReady;
    private string _currentLanguage = "english";
    
    private readonly List<AchievementDefinition> _achievementDefinitions = [];
    private readonly List<StatDefinition> _statDefinitions = [];
    private readonly Dictionary<string, AchievementModel> _achievementPool = new(StringComparer.Ordinal);

    public bool IsReady => _isReady;
    public long GameId => _gameId;

    public event EventHandler<StatsReceivedEventArgs>? StatsReceived;

    public AchievementService(ISteamService steamService, ISteamCallbackService callbackService)
    {
        _steamService = steamService;
        _callbackService = callbackService;
    }

    public async Task InitializeAsync(long gameId, CancellationToken cancellationToken = default)
    {
        Log.Debug($"InitializeAsync called with gameId={gameId}");
        _gameId = gameId;
        _isReady = false;

        // Reinitialize SteamService with this game's AppID
        // This disposes the old client and creates a new one
        Log.Debug("Calling InitializeForGame...");
        _steamService.InitializeForGame(gameId);
        Log.Debug($"InitializeForGame completed. IsInitialized={_steamService.IsInitialized}");

        // Set up callback service with new client
        if (_steamService.Client != null)
        {
            _callbackService.SetClient(_steamService.Client);
            _callbackService.StartCallbackLoop();
        }

        // Get current language
        _currentLanguage = _steamService.SteamApps008?.GetCurrentGameLanguage() ?? "english";
        Log.Debug($"Current language: {_currentLanguage}");

        // Load schema
        Log.Debug("Loading schema...");
        var (success, errorReason) = LoadUserGameStatsSchemaWithReason();
        Log.Debug($"Schema load result: success={success}, errorReason={errorReason}");
        if (!success)
        {
            throw new InvalidOperationException($"Failed to load game stats schema: {errorReason}");
        }

        // Request user stats asynchronously with retry and timeout support
        var steamId = _steamService.SteamId;
        Log.Debug($"Requesting user stats for SteamId={steamId}");
        
        var result = await _callbackService.RequestUserStatsAsync(steamId, cancellationToken);
        
        if (result.Success)
        {
            _isReady = true;
            Log.Debug($"InitializeAsync completed. Achievement count: {_achievementDefinitions.Count}");
            OnStatsReceived(true, result.ResultCode);
        }
        else
        {
            var errorMsg = result.ErrorMessage ?? "Unknown error";
            if (result.RetryCount > 0)
            {
                errorMsg += $" (after {result.RetryCount} retries)";
            }
            Log.Error($"Failed to receive user stats: {errorMsg}");
            OnStatsReceived(false, result.ResultCode, errorMsg);
            throw new InvalidOperationException($"Failed to receive user stats: {errorMsg}");
        }
    }

    private (bool Success, string? ErrorReason) LoadUserGameStatsSchemaWithReason()
    {
        string path;
        try
        {
            string fileName = $"UserGameStatsSchema_{_gameId}.bin";
            var steamPath = Steam.GetInstallPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                return (false, "Steam install path is empty or null");
            }
            path = Path.Combine(steamPath, "appcache", "stats", fileName);
            if (!File.Exists(path))
            {
                return (false, $"Schema file not found: {path}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error getting schema path: {ex.Message}");
        }

        var kv = KeyValue.LoadAsBinary(path);
        if (kv == null)
        {
            return (false, $"KeyValue.LoadAsBinary returned null for: {path}");
        }

        _achievementDefinitions.Clear();
        _statDefinitions.Clear();

        var stats = kv[_gameId.ToString(CultureInfo.InvariantCulture)]["stats"];
        if (stats.Valid == false || stats.Children == null)
        {
            return (false, $"No valid stats found in schema for game {_gameId}");
        }

        foreach (var stat in stats.Children)
        {
            if (stat.Valid == false)
            {
                continue;
            }

            var rawType = stat["type_int"].Valid
                ? stat["type_int"].AsInteger(0)
                : stat["type"].AsInteger(0);
            var type = (UserStatType)rawType;

            switch (type)
            {
                case UserStatType.Invalid:
                    break;

                case UserStatType.Integer:
                {
                    var id = stat["name"].AsString("");
                    string name = GetLocalizedString(stat["display"]["name"], _currentLanguage, id);

                    _statDefinitions.Add(new StatDefinition
                    {
                        Id = id,
                        DisplayName = name,
                        IsFloat = false,
                        MinValue = stat["min"].AsInteger(int.MinValue),
                        MaxValue = stat["max"].AsInteger(int.MaxValue),
                        IncrementOnly = stat["incrementonly"].AsBoolean(false),
                        DefaultValue = stat["default"].AsInteger(0),
                        Permission = stat["permission"].AsInteger(0),
                    });
                    break;
                }

                case UserStatType.Float:
                case UserStatType.AverageRate:
                {
                    var id = stat["name"].AsString("");
                    string name = GetLocalizedString(stat["display"]["name"], _currentLanguage, id);

                    _statDefinitions.Add(new StatDefinition
                    {
                        Id = id,
                        DisplayName = name,
                        IsFloat = true,
                        MinValueFloat = stat["min"].AsFloat(float.MinValue),
                        MaxValueFloat = stat["max"].AsFloat(float.MaxValue),
                        IncrementOnly = stat["incrementonly"].AsBoolean(false),
                        DefaultValueFloat = stat["default"].AsFloat(0.0f),
                        Permission = stat["permission"].AsInteger(0),
                    });
                    break;
                }

                case UserStatType.Achievements:
                case UserStatType.GroupAchievements:
                {
                    if (stat.Children != null)
                    {
                        foreach (var bits in stat.Children.Where(
                            b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
                        {
                            if (bits.Valid == false || bits.Children == null)
                            {
                                continue;
                            }

                            foreach (var bit in bits.Children)
                            {
                                string id = bit["name"].AsString("");
                                string name = GetLocalizedString(bit["display"]["name"], _currentLanguage, id);
                                string desc = GetLocalizedString(bit["display"]["desc"], _currentLanguage, "");

                                _achievementDefinitions.Add(new AchievementDefinition
                                {
                                    Id = id,
                                    Name = name,
                                    Description = desc,
                                    IconNormal = bit["display"]["icon"].AsString(""),
                                    IconLocked = bit["display"]["icon_gray"].AsString(""),
                                    IsHidden = bit["display"]["hidden"].AsBoolean(false),
                                    Permission = bit["permission"].AsInteger(0),
                                });
                            }
                        }
                    }
                    break;
                }
            }
        }

        return (true, null);
    }

    private static string GetLocalizedString(KeyValue kv, string language, string defaultValue)
    {
        var name = kv[language].AsString("");
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (language != "english")
        {
            name = kv["english"].AsString("");
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }

        name = kv.AsString("");
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return defaultValue;
    }

    public Task<IEnumerable<AchievementModel>> GetAchievementsAsync(CancellationToken cancellationToken = default)
    {
        if (_steamService.SteamUserStats is null)
        {
            return Task.FromResult<IEnumerable<AchievementModel>>([]);
        }

        var achievements = new List<AchievementModel>();

        foreach (var def in _achievementDefinitions)
        {
            if (string.IsNullOrEmpty(def.Id))
            {
                continue;
            }

            if (!_steamService.SteamUserStats.GetAchievementAndUnlockTime(def.Id, out bool isAchieved, out var unlockTime))
            {
                continue;
            }

            if (!_achievementPool.TryGetValue(def.Id, out var achievement))
            {
                achievement = new AchievementModel { Id = def.Id };
                _achievementPool[def.Id] = achievement;
            }

            achievement.Name = def.Name.StartsWith("#") ? def.Id : def.Name;
            achievement.Description = def.Description;
            achievement.IsUnlocked = isAchieved;
            achievement.UnlockTime = isAchieved && unlockTime > 0 
                ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime 
                : null;
            achievement.IconUrl = $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{_gameId}/{(isAchieved ? def.IconNormal : def.IconLocked)}";
            achievement.IconLockedUrl = $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{_gameId}/{def.IconLocked}";
            achievement.IsHidden = def.IsHidden;
            achievement.IsProtected = (def.Permission & 3) != 0;
            achievement.IsModified = false;

            achievements.Add(achievement);
        }

        return Task.FromResult<IEnumerable<AchievementModel>>(achievements);
    }

    public Task<IEnumerable<StatModel>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (_steamService.SteamUserStats is null)
        {
            return Task.FromResult<IEnumerable<StatModel>>([]);
        }

        var statistics = new List<StatModel>();

        foreach (var def in _statDefinitions)
        {
            if (string.IsNullOrEmpty(def.Id))
            {
                continue;
            }

            if (!def.IsFloat)
            {
                if (!_steamService.SteamUserStats.GetStatValue(def.Id, out int value))
                {
                    continue;
                }

                statistics.Add(new IntStatModel
                {
                    Id = def.Id,
                    DisplayName = def.DisplayName,
                    IntValue = value,
                    OriginalValue = value,
                    IsIncrementOnly = def.IncrementOnly,
                    IsProtected = (def.Permission & 2) != 0,
                    Permission = def.Permission,
                });
            }
            else
            {
                if (!_steamService.SteamUserStats.GetStatValue(def.Id, out float value))
                {
                    continue;
                }

                statistics.Add(new FloatStatModel
                {
                    Id = def.Id,
                    DisplayName = def.DisplayName,
                    FloatValue = value,
                    OriginalValue = value,
                    IsIncrementOnly = def.IncrementOnly,
                    IsProtected = (def.Permission & 2) != 0,
                    Permission = def.Permission,
                });
            }
        }

        return Task.FromResult<IEnumerable<StatModel>>(statistics);
    }

    public bool SetAchievement(string achievementId, bool unlock)
    {
        if (_steamService.SteamUserStats is null)
        {
            return false;
        }

        return _steamService.SteamUserStats.SetAchievement(achievementId, unlock);
    }

    public bool SetStatistic(string statId, object value)
    {
        if (_steamService.SteamUserStats is null)
        {
            return false;
        }

        return value switch
        {
            int intValue => _steamService.SteamUserStats.SetStatValue(statId, intValue),
            float floatValue => _steamService.SteamUserStats.SetStatValue(statId, floatValue),
            _ => false
        };
    }

    public bool StoreStats()
    {
        return _steamService.SteamUserStats?.StoreStats() ?? false;
    }

    public bool ResetAllStats(bool includeAchievements)
    {
        return _steamService.SteamUserStats?.ResetAllStats(includeAchievements) ?? false;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var steamId = _steamService.SteamId;
        Log.Debug($"RefreshAsync: Requesting user stats for SteamId={steamId}");
        
        var result = await _callbackService.RequestUserStatsAsync(steamId, cancellationToken);
        
        if (result.Success)
        {
            _isReady = true;
            Log.Debug("RefreshAsync: Stats refreshed successfully");
            OnStatsReceived(true, result.ResultCode);
        }
        else
        {
            var errorMsg = result.ErrorMessage ?? "Unknown error";
            if (result.RetryCount > 0)
            {
                errorMsg += $" (after {result.RetryCount} retries)";
            }
            Log.Error($"RefreshAsync: Failed to receive user stats: {errorMsg}");
            OnStatsReceived(false, result.ResultCode, errorMsg);
        }
    }

    protected virtual void OnStatsReceived(bool success, int resultCode, string? errorMessage = null)
    {
        _isReady = success;
        StatsReceived?.Invoke(this, new StatsReceivedEventArgs 
        { 
            Success = success, 
            ResultCode = resultCode, 
            ErrorMessage = errorMessage 
        });
    }

    // Internal classes for schema parsing
    private class AchievementDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconNormal { get; set; } = "";
        public string IconLocked { get; set; } = "";
        public bool IsHidden { get; set; }
        public int Permission { get; set; }
    }

    private class StatDefinition
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsFloat { get; set; }
        public int MinValue { get; set; } = int.MinValue;
        public int MaxValue { get; set; } = int.MaxValue;
        public float MinValueFloat { get; set; } = float.MinValue;
        public float MaxValueFloat { get; set; } = float.MaxValue;
        public bool IncrementOnly { get; set; }
        public int DefaultValue { get; set; }
        public float DefaultValueFloat { get; set; }
        public int Permission { get; set; }
    }
}
