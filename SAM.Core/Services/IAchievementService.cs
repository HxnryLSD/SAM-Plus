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
/// Service for managing achievements and statistics for a specific game.
/// </summary>
public interface IAchievementService
{
    /// <summary>
    /// Gets whether the service is ready (stats loaded).
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Gets the current game ID.
    /// </summary>
    long GameId { get; }

    /// <summary>
    /// Initializes the service for a specific game.
    /// </summary>
    Task InitializeAsync(long gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all achievements for the current game.
    /// </summary>
    Task<IEnumerable<AchievementModel>> GetAchievementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all statistics for the current game.
    /// </summary>
    Task<IEnumerable<StatModel>> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks or locks an achievement.
    /// </summary>
    bool SetAchievement(string achievementId, bool unlock);

    /// <summary>
    /// Sets a statistic value.
    /// </summary>
    bool SetStatistic(string statId, object value);

    /// <summary>
    /// Stores all pending changes to Steam.
    /// </summary>
    bool StoreStats();

    /// <summary>
    /// Resets all statistics (and optionally achievements).
    /// </summary>
    bool ResetAllStats(bool includeAchievements);

    /// <summary>
    /// Refreshes achievements and statistics from Steam.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when stats are received from Steam.
    /// </summary>
    event EventHandler<StatsReceivedEventArgs>? StatsReceived;
}

public class StatsReceivedEventArgs : EventArgs
{
    public bool Success { get; init; }
    public int ResultCode { get; init; }
    public string? ErrorMessage { get; init; }
}
