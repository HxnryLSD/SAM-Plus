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

namespace SAM.API
{
    /// <summary>
    /// Caches information about games with protected achievements/stats.
    /// This cache is populated by SAM.Game when analyzing a game and read by SAM.Picker.
    /// Protection is determined by Steam's schema (permission & 2 flag).
    /// </summary>
    public static class ProtectionCache
    {
        private static readonly string CacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SAM-Plus", "Cache");

        private static readonly string CacheFile = Path.Combine(CacheDir, "protection_cache.json");

        private static Dictionary<uint, ProtectionInfo> _cache = new();
        private static bool _isLoaded = false;
        private static readonly object _lock = new();

        /// <summary>
        /// Loads the protection cache from disk.
        /// </summary>
        public static void Load()
        {
            lock (_lock)
            {
                if (_isLoaded) return;

                try
                {
                    if (File.Exists(CacheFile))
                    {
                        var json = File.ReadAllText(CacheFile);
                        var entries = JsonSerializer.Deserialize<ProtectionInfo[]>(json);
                        if (entries != null)
                        {
                            _cache.Clear();
                            foreach (var entry in entries)
                            {
                                _cache[entry.AppId] = entry;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to load protection cache: {ex.Message}");
                }

                _isLoaded = true;
            }
        }

        /// <summary>
        /// Saves the protection cache to disk.
        /// </summary>
        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(CacheDir);

                    var entries = new List<ProtectionInfo>(_cache.Values);
                    var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(CacheFile, json);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to save protection cache: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Updates the protection status for a game.
        /// Called by SAM.Game after loading the schema.
        /// </summary>
        public static void UpdateProtectionStatus(uint appId, int protectedAchievements, int protectedStats, int totalAchievements, int totalStats)
        {
            lock (_lock)
            {
                if (!_isLoaded) Load();

                _cache[appId] = new ProtectionInfo
                {
                    AppId = appId,
                    ProtectedAchievements = protectedAchievements,
                    ProtectedStats = protectedStats,
                    TotalAchievements = totalAchievements,
                    TotalStats = totalStats,
                    LastChecked = DateTime.UtcNow
                };

                // Save immediately
                Save();
            }
        }

        /// <summary>
        /// Gets the protection info for a game.
        /// Returns null if the game hasn't been analyzed yet.
        /// </summary>
        public static ProtectionInfo GetProtectionInfo(uint appId)
        {
            lock (_lock)
            {
                if (!_isLoaded) Load();

                return _cache.TryGetValue(appId, out var info) ? info : null;
            }
        }

        /// <summary>
        /// Checks if a game has any protected achievements or stats.
        /// Returns null if unknown (game not yet analyzed).
        /// </summary>
        public static bool? HasProtection(uint appId)
        {
            var info = GetProtectionInfo(appId);
            if (info == null) return null;
            return info.ProtectedAchievements > 0 || info.ProtectedStats > 0;
        }

        /// <summary>
        /// Information about a game's protection status.
        /// </summary>
        public class ProtectionInfo
        {
            public uint AppId { get; set; }
            public int ProtectedAchievements { get; set; }
            public int ProtectedStats { get; set; }
            public int TotalAchievements { get; set; }
            public int TotalStats { get; set; }
            public DateTime LastChecked { get; set; }

            public bool IsProtected => ProtectedAchievements > 0 || ProtectedStats > 0;
        }
    }
}
