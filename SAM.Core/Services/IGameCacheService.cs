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

using SAM.Core.Models;

namespace SAM.Core.Services;

/// <summary>
/// Cached game information stored in SQLite.
/// </summary>
public record CachedGameInfo
{
    public uint AppId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AchievementCount { get; init; }
    public int UnlockedCount { get; init; }
    public bool HasDrm { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime LastUpdated { get; init; }
    public DateTime? LastPlayed { get; init; }
    public int PlaytimeMinutes { get; init; }
}

/// <summary>
/// Cache statistics.
/// </summary>
public record CacheStatistics
{
    public int TotalGames { get; init; }
    public long DatabaseSizeBytes { get; init; }
    public DateTime? OldestEntry { get; init; }
    public DateTime? NewestEntry { get; init; }
}

/// <summary>
/// Service for caching game metadata in SQLite database.
/// Provides fast offline access to game information.
/// </summary>
public interface IGameCacheService
{
    /// <summary>
    /// Gets a cached game by App ID.
    /// </summary>
    Task<CachedGameInfo?> GetGameAsync(uint appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cached games.
    /// </summary>
    Task<IReadOnlyList<CachedGameInfo>> GetAllGamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cached games for a specific user (by steam ID).
    /// </summary>
    Task<IReadOnlyList<CachedGameInfo>> GetGamesForUserAsync(string steamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates a game in the cache.
    /// </summary>
    Task SaveGameAsync(CachedGameInfo game, string? steamId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple games at once (batch operation).
    /// </summary>
    Task SaveGamesAsync(IEnumerable<CachedGameInfo> games, string? steamId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates achievement counts for a game.
    /// </summary>
    Task UpdateAchievementCountsAsync(uint appId, int total, int unlocked, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a game from the cache.
    /// </summary>
    Task RemoveGameAsync(uint appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches games by name.
    /// </summary>
    Task<IReadOnlyList<CachedGameInfo>> SearchGamesAsync(string query, CancellationToken cancellationToken = default);
}
