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

using System.Globalization;
using SAM.API;
using SAM.API.Types;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Information about DRM protection for a game.
/// </summary>
public class DrmProtectionInfo
{
    /// <summary>
    /// Whether the game has any protected achievements.
    /// </summary>
    public bool HasProtection { get; init; }
    
    /// <summary>
    /// Total number of achievements in the game.
    /// </summary>
    public int TotalAchievements { get; init; }
    
    /// <summary>
    /// Number of protected achievements.
    /// </summary>
    public int ProtectedCount { get; init; }
    
    /// <summary>
    /// Human-readable description of the protection status.
    /// </summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Interface for DRM protection detection service.
/// </summary>
public interface IDrmProtectionService
{
    /// <summary>
    /// Checks if a game has DRM-protected achievements by reading the local schema file.
    /// This does NOT use the Steam API, only reads cached schema files.
    /// </summary>
    /// <param name="gameId">The Steam App ID of the game.</param>
    /// <returns>DRM protection information, or null if schema file not found.</returns>
    DrmProtectionInfo? CheckGameProtection(uint gameId);
    
    /// <summary>
    /// Gets the path to the schema file for a game.
    /// </summary>
    /// <param name="gameId">The Steam App ID of the game.</param>
    /// <returns>Full path to schema file, or null if not found.</returns>
    string? GetSchemaFilePath(uint gameId);
}

/// <summary>
/// Service that detects DRM-protected achievements by reading Steam's cached schema files.
/// This approach does NOT require Steam API calls and works offline.
/// </summary>
public class DrmProtectionService : IDrmProtectionService
{
    private readonly string? _steamInstallPath;
    private readonly Dictionary<uint, DrmProtectionInfo> _cache = [];

    public DrmProtectionService()
    {
        try
        {
            _steamInstallPath = Steam.GetInstallPath();
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not get Steam install path: {ex.Message}");
            _steamInstallPath = null;
        }
    }

    public string? GetSchemaFilePath(uint gameId)
    {
        if (string.IsNullOrEmpty(_steamInstallPath))
        {
            return null;
        }

        var fileName = $"UserGameStatsSchema_{gameId}.bin";
        var path = Path.Combine(_steamInstallPath, "appcache", "stats", fileName);
        
        return File.Exists(path) ? path : null;
    }

    public DrmProtectionInfo? CheckGameProtection(uint gameId)
    {
        // Check cache first
        if (_cache.TryGetValue(gameId, out var cached))
        {
            return cached;
        }

        var schemaPath = GetSchemaFilePath(gameId);
        if (schemaPath == null)
        {
            // No schema file = no protection info available
            // This is normal for games that haven't been played yet
            return null;
        }

        try
        {
            var result = AnalyzeSchemaFile(gameId, schemaPath);
            _cache[gameId] = result;
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn($"Error analyzing schema for game {gameId}: {ex.Message}");
            return null;
        }
    }

    private static DrmProtectionInfo AnalyzeSchemaFile(uint gameId, string schemaPath)
    {
        var kv = KeyValue.LoadAsBinary(schemaPath);
        if (kv == null)
        {
            return new DrmProtectionInfo
            {
                HasProtection = false,
                TotalAchievements = 0,
                ProtectedCount = 0,
                Description = "Schema konnte nicht gelesen werden"
            };
        }

        var stats = kv[gameId.ToString(CultureInfo.InvariantCulture)]["stats"];
        if (!stats.Valid || stats.Children == null)
        {
            return new DrmProtectionInfo
            {
                HasProtection = false,
                TotalAchievements = 0,
                ProtectedCount = 0,
                Description = "Keine Stats-Daten gefunden"
            };
        }

        int totalAchievements = 0;
        int protectedCount = 0;

        foreach (var stat in stats.Children)
        {
            if (!stat.Valid)
            {
                continue;
            }

            var rawType = stat["type_int"].Valid
                ? stat["type_int"].AsInteger(0)
                : stat["type"].AsInteger(0);
            var type = (UserStatType)rawType;

            // Only process achievement types
            if (type != UserStatType.Achievements && type != UserStatType.GroupAchievements)
            {
                continue;
            }

            if (stat.Children == null)
            {
                continue;
            }

            foreach (var bits in stat.Children.Where(
                b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
            {
                if (!bits.Valid || bits.Children == null)
                {
                    continue;
                }

                foreach (var bit in bits.Children)
                {
                    totalAchievements++;
                    
                    int permission = bit["permission"].AsInteger(0);
                    // Protection bits: bit 0 and bit 1 (mask 0x3)
                    // If either is set, the achievement is protected
                    if ((permission & 3) != 0)
                    {
                        protectedCount++;
                    }
                }
            }
        }

        bool hasProtection = protectedCount > 0;
        string description;

        if (!hasProtection)
        {
            description = string.Empty;
        }
        else if (protectedCount == totalAchievements)
        {
            description = $"Alle {totalAchievements} Erfolge sind geschützt und können nicht bearbeitet werden.";
        }
        else
        {
            description = $"{protectedCount} von {totalAchievements} Erfolgen sind geschützt und können nicht bearbeitet werden.";
        }

        return new DrmProtectionInfo
        {
            HasProtection = hasProtection,
            TotalAchievements = totalAchievements,
            ProtectedCount = protectedCount,
            Description = description
        };
    }
}
