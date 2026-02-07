/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SAM.API
{
    /// <summary>
    /// Local cache for game list data to avoid repeated downloads.
    /// Stores game IDs and types in a JSON file with TTL-based expiration.
    /// </summary>
    public static class GameListCache
    {
        private static readonly string CacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SAM-Plus", "Cache");

        private static readonly string CacheFile = Path.Combine(CacheDir, "games_cache.json");

        /// <summary>
        /// Cache time-to-live in hours (default: 24 hours)
        /// </summary>
        public static int CacheTtlHours { get; set; } = 24;

        /// <summary>
        /// Represents a cached game entry.
        /// </summary>
        public record GameCacheEntry(uint Id, string Type);

        /// <summary>
        /// Represents the cache data structure.
        /// </summary>
        private class CacheData
        {
            public DateTime LastUpdated { get; set; }
            public List<GameCacheEntry> Games { get; set; } = new();
        }

        /// <summary>
        /// Tries to load games from the local cache.
        /// </summary>
        /// <returns>Cached game list if valid, null if cache is expired or missing.</returns>
        public static async Task<List<GameCacheEntry>> TryLoadFromCacheAsync()
        {
            try
            {
                if (!File.Exists(CacheFile))
                    return null;

                var json = await File.ReadAllTextAsync(CacheFile);
                var cache = JsonSerializer.Deserialize<CacheData>(json);

                if (cache == null || cache.Games == null)
                    return null;

                // Check if cache is expired
                if (DateTime.UtcNow - cache.LastUpdated > TimeSpan.FromHours(CacheTtlHours))
                {
                    Logger.Info($"Game cache expired (last updated: {cache.LastUpdated})");
                    return null;
                }

                Logger.Info($"Loaded {cache.Games.Count} games from cache");
                return cache.Games;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to load game cache: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves the game list to the local cache.
        /// </summary>
        public static async Task SaveToCacheAsync(List<GameCacheEntry> games)
        {
            try
            {
                Directory.CreateDirectory(CacheDir);

                var cache = new CacheData
                {
                    LastUpdated = DateTime.UtcNow,
                    Games = games
                };

                var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions
                {
                    WriteIndented = false // Keep it compact
                });

                await File.WriteAllTextAsync(CacheFile, json);
                Logger.Info($"Saved {games.Count} games to cache");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to save game cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Invalidates the cache by deleting the cache file.
        /// </summary>
        public static void Invalidate()
        {
            try
            {
                if (File.Exists(CacheFile))
                {
                    File.Delete(CacheFile);
                    Logger.Info("Game cache invalidated");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to invalidate game cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the cache file path for diagnostics.
        /// </summary>
        public static string GetCachePath() => CacheFile;

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public static (bool Exists, DateTime? LastUpdated, int? GameCount, long? FileSizeBytes) GetCacheStats()
        {
            if (!File.Exists(CacheFile))
                return (false, null, null, null);

            try
            {
                var fileInfo = new FileInfo(CacheFile);
                var json = File.ReadAllText(CacheFile);
                var cache = JsonSerializer.Deserialize<CacheData>(json);

                return (true, cache?.LastUpdated, cache?.Games?.Count, fileInfo.Length);
            }
            catch
            {
                return (true, null, null, null);
            }
        }
    }
}
