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
using System.Globalization;
using SAM.API;
using SAM.API.Types;
using SAM.Core.Models;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Service for fetching complete library game data including achievements and DRM status.
/// </summary>
public class LibraryFetchService : ILibraryFetchService
{
    private readonly ISteamService _steamService;
    private readonly IUserDataService _userDataService;
    private readonly IDrmProtectionService _drmService;
    private readonly IGameCacheService _gameCacheService;
    private readonly string? _steamInstallPath;
    
    private bool _isFetching;
    private LibraryFetchResult? _lastResult;
    private DateTime? _lastFetchTime;

    public bool IsFetching => _isFetching;
    public LibraryFetchResult? LastResult => _lastResult;
    public DateTime? LastFetchTime => _lastFetchTime;

    public event EventHandler<LibraryFetchProgress>? FetchProgress;
    public event EventHandler<LibraryFetchResult>? FetchCompleted;

    public LibraryFetchService(
        ISteamService steamService, 
        IUserDataService userDataService,
        IDrmProtectionService drmService,
        IGameCacheService gameCacheService)
    {
        _steamService = steamService;
        _userDataService = userDataService;
        _drmService = drmService;
        _gameCacheService = gameCacheService;
        
        try
        {
            _steamInstallPath = Steam.GetInstallPath();
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not get Steam install path: {ex.Message}");
        }
    }

    public async Task<LibraryFetchResult> FetchAllGamesAsync(
        IProgress<LibraryFetchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_isFetching)
        {
            throw new InvalidOperationException("A fetch operation is already in progress.");
        }

        _isFetching = true;
        var stopwatch = Stopwatch.StartNew();
        var failedGames = new Dictionary<uint, string>();
        int successCount = 0;
        int skippedCount = 0;

        try
        {
            // Ensure Steam service is initialized
            if (!_steamService.IsInitialized)
            {
                _steamService.Initialize(0);
            }
            
            // Set current user for UserDataService
            var steamId = _steamService.SteamId;
            if (steamId > 0)
            {
                _userDataService.SetCurrentUser(steamId.ToString());
            }
            else
            {
                throw new InvalidOperationException("Steam user ID not available.");
            }

            // Get owned games
            Log.Info("LibraryFetchService: Getting owned games list...");
            var games = (await _steamService.GetOwnedGamesAsync(cancellationToken)).ToList();
            Log.Info($"LibraryFetchService: Found {games.Count} owned games.");
            
            int totalGames = games.Count;

            for (int i = 0; i < games.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var game = games[i];
                var progressInfo = new LibraryFetchProgress
                {
                    CurrentGameName = game.Name,
                    CurrentGameId = game.Id,
                    CurrentIndex = i + 1,
                    TotalGames = totalGames,
                    IsSuccess = true
                };

                try
                {
                    // Check DRM protection from local schema
                    var drmInfo = _drmService.CheckGameProtection(game.Id);
                    
                    if (drmInfo == null)
                    {
                        // No schema file found - try to load it by initializing the game briefly
                        var achievementInfo = await TryLoadGameAchievementsAsync(game.Id, cancellationToken);
                        
                        if (achievementInfo != null)
                        {
                            await SaveGameDataAsync(game, achievementInfo.Value.totalCount, 
                                achievementInfo.Value.unlockedCount, achievementInfo.Value.protectedCount);
                            successCount++;
                        }
                        else
                        {
                            // Couldn't load achievement data - save basic info
                            await SaveGameDataAsync(game, 0, 0, 0, hasSchemaFile: false);
                            skippedCount++;
                        }
                    }
                    else
                    {
                        // We have DRM info from schema, now try to get unlock status
                        var achievementInfo = await TryLoadGameAchievementsAsync(game.Id, cancellationToken);
                        
                        var totalCount = achievementInfo?.totalCount ?? drmInfo.TotalAchievements;
                        var unlockedCount = achievementInfo?.unlockedCount ?? 0;
                        var protectedCount = achievementInfo?.protectedCount ?? drmInfo.ProtectedCount;
                        
                        await SaveGameDataAsync(game, totalCount, unlockedCount, protectedCount);
                        successCount++;
                    }

                    progressInfo = progressInfo with { IsSuccess = true };
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to fetch data for game {game.Name} ({game.Id}): {ex.Message}");
                    failedGames[game.Id] = ex.Message;
                    progressInfo = progressInfo with { IsSuccess = false, ErrorMessage = ex.Message };
                }

                // Report progress
                progress?.Report(progressInfo);
                FetchProgress?.Invoke(this, progressInfo);
                
                // Small delay to avoid overwhelming Steam
                await Task.Delay(50, cancellationToken);
            }

            stopwatch.Stop();

            _lastResult = new LibraryFetchResult
            {
                TotalGames = totalGames,
                SuccessCount = successCount,
                FailedCount = failedGames.Count,
                SkippedCount = skippedCount,
                Duration = stopwatch.Elapsed,
                WasCancelled = false,
                FailedGames = failedGames
            };
            _lastFetchTime = DateTime.UtcNow;
            
            FetchCompleted?.Invoke(this, _lastResult);
            
            Log.Info($"LibraryFetchService: Completed. Success: {successCount}, Failed: {failedGames.Count}, Skipped: {skippedCount}, Duration: {stopwatch.Elapsed}");
            
            return _lastResult;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            
            _lastResult = new LibraryFetchResult
            {
                TotalGames = 0,
                SuccessCount = successCount,
                FailedCount = failedGames.Count,
                SkippedCount = skippedCount,
                Duration = stopwatch.Elapsed,
                WasCancelled = true,
                FailedGames = failedGames
            };
            
            FetchCompleted?.Invoke(this, _lastResult);
            
            Log.Info("LibraryFetchService: Operation cancelled.");
            
            return _lastResult;
        }
        finally
        {
            _isFetching = false;
        }
    }

    private async Task<(int totalCount, int unlockedCount, int protectedCount)?> TryLoadGameAchievementsAsync(
        uint gameId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Read schema file directly without initializing Steam for this game
            var schemaPath = GetSchemaFilePath(gameId);
            if (string.IsNullOrEmpty(schemaPath) || !File.Exists(schemaPath))
            {
                return null;
            }

            var kv = KeyValue.LoadAsBinary(schemaPath);
            if (kv == null)
            {
                return null;
            }

            var stats = kv[gameId.ToString(CultureInfo.InvariantCulture)]["stats"];
            if (stats.Valid == false || stats.Children == null)
            {
                return null;
            }

            int totalCount = 0;
            int protectedCount = 0;

            foreach (var stat in stats.Children)
            {
                if (stat.Valid == false || stat.Children == null)
                {
                    continue;
                }

                var rawType = stat["type_int"].Valid 
                    ? stat["type_int"].AsInteger(0) 
                    : stat["type"].AsInteger(0);
                var statType = (UserStatType)rawType;

                if (statType != UserStatType.Achievements)
                {
                    continue;
                }

                var bits = stat["bits"];
                if (bits.Valid == false || bits.Children == null)
                {
                    continue;
                }

                foreach (var bit in bits.Children)
                {
                    if (bit.Valid == false)
                    {
                        continue;
                    }

                    totalCount++;

                    // Check for protection
                    var permission = bit["permission"].AsInteger(0);
                    if (permission != 0)
                    {
                        protectedCount++;
                    }
                }
            }

            // For unlocked count, we'd need to actually check Steam stats
            // which requires full initialization. For now, we'll leave it at 0
            // unless we can get it from saved user data
            var existingData = await _userDataService.GetGameDataAsync(gameId, cancellationToken);
            int unlockedCount = existingData?.UnlockedAchievementCount ?? 0;

            return (totalCount, unlockedCount, protectedCount);
        }
        catch (Exception ex)
        {
            Log.Debug($"Could not load achievements for game {gameId}: {ex.Message}");
            return null;
        }
    }

    private string? GetSchemaFilePath(uint gameId)
    {
        if (string.IsNullOrEmpty(_steamInstallPath))
        {
            return null;
        }

        string fileName = $"UserGameStatsSchema_{gameId}.bin";
        var path = Path.Combine(_steamInstallPath, "appcache", "stats", fileName);
        return File.Exists(path) ? path : null;
    }

    private async Task SaveGameDataAsync(
        GameModel game, 
        int totalAchievements, 
        int unlockedAchievements, 
        int protectedAchievements,
        bool hasSchemaFile = true)
    {
        var gameData = new GameUserData
        {
            GameId = game.Id,
            GameName = game.Name,
            HasDrmProtection = protectedAchievements > 0,
            ProtectedAchievementCount = protectedAchievements,
            TotalAchievementCount = totalAchievements,
            UnlockedAchievementCount = unlockedAchievements,
            CompletionPercentage = totalAchievements > 0 
                ? (double)unlockedAchievements / totalAchievements * 100 
                : 0,
            DrmProtectionInfo = protectedAchievements > 0 
                ? $"{protectedAchievements} von {totalAchievements} Erfolgen sind geschützt" 
                : null,
            IsInitialized = hasSchemaFile,
            LastUpdated = DateTime.UtcNow
        };

        await _userDataService.SaveGameDataAsync(gameData);
        
        // Also save to SQLite cache for fast access
        var cachedGame = new CachedGameInfo
        {
            AppId = game.Id,
            Name = game.Name,
            AchievementCount = totalAchievements,
            UnlockedCount = unlockedAchievements,
            HasDrm = protectedAchievements > 0,
            ImageUrl = game.ImageUrl,
            LastUpdated = DateTime.UtcNow,
            LastPlayed = null, // Could be populated from Steam API
            PlaytimeMinutes = 0 // Could be populated from Steam API
        };
        
        try
        {
            var steamId = _steamService.SteamId.ToString();
            await _gameCacheService.SaveGameAsync(cachedGame, steamId);
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to save game to SQLite cache: {ex.Message}");
        }
        
        Log.Debug($"Saved game data: {game.Name} ({game.Id}) - {totalAchievements} achievements, {protectedAchievements} protected");
    }
}
