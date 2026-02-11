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

using SAM.API;
using SAM.API.Wrappers;
using SAM.Core.Models;

namespace SAM.Core.Services;

/// <summary>
/// Service for managing Steam client connection and game listing.
/// </summary>
public interface ISteamService : IDisposable
{
    /// <summary>
    /// Gets whether the Steam client is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the current AppID the client is initialized with.
    /// </summary>
    long CurrentAppId { get; }

    /// <summary>
    /// Gets the current Steam user's ID.
    /// </summary>
    ulong SteamId { get; }

    /// <summary>
    /// Gets the underlying Steam client for callback registration.
    /// </summary>
    Client? Client { get; }

    /// <summary>
    /// Gets the SteamUserStats wrapper for achievement/stat operations.
    /// </summary>
    SteamUserStats013? SteamUserStats { get; }

    /// <summary>
    /// Gets the SteamApps008 wrapper for app operations.
    /// </summary>
    SteamApps008? SteamApps008 { get; }

    /// <summary>
    /// Initializes the Steam client connection.
    /// </summary>
    /// <param name="appId">Optional app ID to initialize with.</param>
    void Initialize(long appId = 0);

    /// <summary>
    /// Reinitializes the Steam client for a specific game.
    /// Disposes the old client and creates a new one with the game's AppID.
    /// </summary>
    /// <param name="gameId">The game's AppID.</param>
    void InitializeForGame(long gameId);

    /// <summary>
    /// Gets a list of owned games with achievements.
    /// </summary>
    Task<IEnumerable<GameModel>> GetOwnedGamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of a game by its ID.
    /// </summary>
    string? GetGameName(uint appId);

    /// <summary>
    /// Gets the current game language.
    /// </summary>
    string? GetCurrentGameLanguage();

    /// <summary>
    /// Checks if the user owns a specific game.
    /// </summary>
    bool OwnsGame(uint appId);

    /// <summary>
    /// Gets the logo URL for a game.
    /// </summary>
    string? GetGameLogoUrl(uint appId);

    /// <summary>
    /// Runs Steam callbacks (must be called periodically).
    /// </summary>
    void RunCallbacks();
}
