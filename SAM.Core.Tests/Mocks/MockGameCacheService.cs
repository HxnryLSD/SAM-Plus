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

using SAM.Core.Services;

namespace SAM.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of IGameCacheService for testing.
/// </summary>
public class MockGameCacheService : IGameCacheService
{
    private readonly Dictionary<uint, CachedGameInfo> _games = new();
    private readonly Dictionary<string, HashSet<uint>> _userGames = new();

    public void SetGame(CachedGameInfo game, string? steamId = null)
    {
        _games[game.AppId] = game;
        if (!string.IsNullOrEmpty(steamId))
        {
            if (!_userGames.ContainsKey(steamId))
            {
                _userGames[steamId] = new HashSet<uint>();
            }
            _userGames[steamId].Add(game.AppId);
        }
    }

    public Task<CachedGameInfo?> GetGameAsync(uint appId, CancellationToken cancellationToken = default)
    {
        _games.TryGetValue(appId, out var game);
        return Task.FromResult(game);
    }

    public Task<IReadOnlyList<CachedGameInfo>> GetAllGamesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CachedGameInfo>>(_games.Values.OrderBy(g => g.Name).ToList());
    }

    public Task<IReadOnlyList<CachedGameInfo>> GetGamesForUserAsync(string steamId, CancellationToken cancellationToken = default)
    {
        if (!_userGames.TryGetValue(steamId, out var userGameIds))
        {
            return Task.FromResult<IReadOnlyList<CachedGameInfo>>(new List<CachedGameInfo>());
        }

        var games = userGameIds
            .Where(id => _games.ContainsKey(id))
            .Select(id => _games[id])
            .OrderBy(g => g.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<CachedGameInfo>>(games);
    }

    public Task SaveGameAsync(CachedGameInfo game, string? steamId = null, CancellationToken cancellationToken = default)
    {
        SetGame(game, steamId);
        return Task.CompletedTask;
    }

    public Task SaveGamesAsync(IEnumerable<CachedGameInfo> games, string? steamId = null, CancellationToken cancellationToken = default)
    {
        foreach (var game in games)
        {
            SetGame(game, steamId);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAchievementCountsAsync(uint appId, int total, int unlocked, CancellationToken cancellationToken = default)
    {
        if (_games.TryGetValue(appId, out var game))
        {
            _games[appId] = game with
            {
                AchievementCount = total,
                UnlockedCount = unlocked,
                LastUpdated = DateTime.UtcNow
            };
        }
        return Task.CompletedTask;
    }

    public Task RemoveGameAsync(uint appId, CancellationToken cancellationToken = default)
    {
        _games.Remove(appId);
        foreach (var userGames in _userGames.Values)
        {
            userGames.Remove(appId);
        }
        return Task.CompletedTask;
    }

    public Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        _games.Clear();
        _userGames.Clear();
        return Task.CompletedTask;
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CacheStatistics
        {
            TotalGames = _games.Count,
            DatabaseSizeBytes = _games.Count * 100, // Approximate
            OldestEntry = _games.Values.Any() ? _games.Values.Min(g => g.LastUpdated) : null,
            NewestEntry = _games.Values.Any() ? _games.Values.Max(g => g.LastUpdated) : null
        });
    }

    public Task<IReadOnlyList<CachedGameInfo>> SearchGamesAsync(string query, CancellationToken cancellationToken = default)
    {
        var games = _games.Values
            .Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<CachedGameInfo>>(games);
    }
}
