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
/// Type of Steam application.
/// </summary>
public enum GameType
{
    Unknown,
    Game,
    Mod,
    Dlc,
    Demo,
    Application,
    Tool,
    Video,
    Config,
    Hardware
}

/// <summary>
/// Represents a Steam game with achievements.
/// </summary>
public class GameModel
{
    public required uint Id { get; init; }
    public required string Name { get; set; }
    public GameType Type { get; set; } = GameType.Unknown;
    public string? ImageUrl { get; set; }
    public int AchievementCount { get; set; }
    public int UnlockedAchievementCount { get; set; }
    
    /// <summary>
    /// Indicates if the game has DRM-protected achievements that cannot be modified.
    /// </summary>
    public bool HasDrmProtection { get; set; }
    
    /// <summary>
    /// Number of protected achievements in this game.
    /// </summary>
    public int ProtectedAchievementCount { get; set; }
    
    /// <summary>
    /// Reason/info text for the DRM protection (shown in tooltip).
    /// </summary>
    public string? DrmProtectionInfo { get; set; }

    public double CompletionPercentage => AchievementCount > 0
        ? (double)UnlockedAchievementCount / AchievementCount * 100
        : 0;
}
