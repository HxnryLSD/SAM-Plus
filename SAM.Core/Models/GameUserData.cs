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

namespace SAM.Core.Models;

/// <summary>
/// Stores user-specific data for a game, including DRM protection info and achievement statistics.
/// This data is persisted per-user per-game in the Userdata folder.
/// </summary>
public class GameUserData
{
    /// <summary>
    /// The Steam App ID of the game.
    /// </summary>
    public uint GameId { get; set; }

    /// <summary>
    /// The game's display name.
    /// </summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the game has DRM protection on achievements.
    /// </summary>
    public bool HasDrmProtection { get; set; }

    /// <summary>
    /// Number of protected achievements (if any).
    /// </summary>
    public int ProtectedAchievementCount { get; set; }

    /// <summary>
    /// Total number of achievements in the game.
    /// </summary>
    public int TotalAchievementCount { get; set; }

    /// <summary>
    /// Number of achievements unlocked by the user.
    /// </summary>
    public int UnlockedAchievementCount { get; set; }

    /// <summary>
    /// Completion percentage (0-100).
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// Description of DRM protection (if applicable).
    /// </summary>
    public string? DrmProtectionInfo { get; set; }

    /// <summary>
    /// Last time this data was updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time achievements were modified by SAM.
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Total playtime in minutes (from Steam).
    /// </summary>
    public int PlaytimeMinutes { get; set; }

    /// <summary>
    /// Whether the game data has been fully loaded at least once.
    /// </summary>
    public bool IsInitialized { get; set; }
}
