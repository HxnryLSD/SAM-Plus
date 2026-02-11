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

using System.Collections.Concurrent;
using System.Text.Json;
using SAM.Core.Models;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Service for managing user-specific game data stored in the Userdata folder.
/// </summary>
public class UserDataService : IUserDataService
{
    private readonly ConcurrentDictionary<uint, GameUserData> _cache = new();
    private string? _currentUserId;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string? CurrentUserId => _currentUserId;

    public void SetCurrentUser(string steamId)
    {
        if (_currentUserId != steamId)
        {
            _cache.Clear();
            _currentUserId = steamId;
            Log.Info($"User data service set to user: {steamId}");
        }
    }

    public async Task<GameUserData?> GetGameDataAsync(uint gameId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            Log.Warn("GetGameDataAsync called without current user set");
            return null;
        }

        // Check cache first
        if (_cache.TryGetValue(gameId, out var cachedData))
        {
            return cachedData;
        }

        // Load from disk
        var filePath = AppPaths.GetGameDataFilePath(_currentUserId, gameId);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var data = JsonSerializer.Deserialize<GameUserData>(json, _jsonOptions);
            
            if (data != null)
            {
                _cache.TryAdd(gameId, data);
            }
            
            return data;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load game data for {gameId}: {ex.Message}");
            return null;
        }
    }

    public async Task SaveGameDataAsync(GameUserData data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            Log.Warn("SaveGameDataAsync called without current user set");
            return;
        }

        data.LastUpdated = DateTime.UtcNow;
        
        var filePath = AppPaths.GetGameDataFilePath(_currentUserId, data.GameId);
        
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            
            // Update cache
            _cache[data.GameId] = data;
            
            Log.Debug($"Saved game data for {data.GameId} ({data.GameName})");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save game data for {data.GameId}: {ex.Message}");
        }
    }

    public async Task<Dictionary<uint, GameUserData>> GetAllGameDataAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            Log.Warn("GetAllGameDataAsync called without current user set");
            return new Dictionary<uint, GameUserData>();
        }

        var result = new Dictionary<uint, GameUserData>();
        var gameIds = AppPaths.GetUserGames(_currentUserId);

        foreach (var gameIdStr in gameIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (uint.TryParse(gameIdStr, out var gameId))
            {
                var data = await GetGameDataAsync(gameId, cancellationToken);
                if (data != null)
                {
                    result[gameId] = data;
                }
            }
        }

        return result;
    }

    public Task DeleteGameDataAsync(uint gameId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            Log.Warn("DeleteGameDataAsync called without current user set");
            return Task.CompletedTask;
        }

        try
        {
            var gamePath = AppPaths.GetGamePath(_currentUserId, gameId);
            
            if (Directory.Exists(gamePath))
            {
                Directory.Delete(gamePath, recursive: true);
                _cache.TryRemove(gameId, out _);
                Log.Info($"Deleted game data for {gameId}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete game data for {gameId}: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public IEnumerable<string> GetAllUsers()
    {
        return AppPaths.GetAllUsers();
    }
}
