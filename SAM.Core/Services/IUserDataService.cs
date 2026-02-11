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
/// Service for managing user-specific game data.
/// </summary>
public interface IUserDataService
{
    /// <summary>
    /// Gets the current Steam user ID.
    /// </summary>
    string? CurrentUserId { get; }

    /// <summary>
    /// Sets the current Steam user ID.
    /// </summary>
    /// <param name="steamId">The Steam ID or username.</param>
    void SetCurrentUser(string steamId);

    /// <summary>
    /// Gets game data for a specific game for the current user.
    /// </summary>
    /// <param name="gameId">The game's App ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The game user data, or null if not found.</returns>
    Task<GameUserData?> GetGameDataAsync(uint gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves game data for a specific game for the current user.
    /// </summary>
    /// <param name="data">The game data to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveGameDataAsync(GameUserData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all game data for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of game ID to game data.</returns>
    Task<Dictionary<uint, GameUserData>> GetAllGameDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes game data for a specific game.
    /// </summary>
    /// <param name="gameId">The game's App ID.</param>
    Task DeleteGameDataAsync(uint gameId);

    /// <summary>
    /// Gets all recognized Steam user IDs.
    /// </summary>
    /// <returns>List of Steam IDs.</returns>
    IEnumerable<string> GetAllUsers();
}
