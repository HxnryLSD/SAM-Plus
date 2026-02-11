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
using SAM.Core.Services;

namespace SAM.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of ISteamService for testing.
/// </summary>
public class MockSteamService : ISteamService
{
    private readonly List<GameModel> _games = [];
    private bool _isInitialized;
    private long _currentAppId;

    public bool IsInitialized => _isInitialized;
    public long CurrentAppId => _currentAppId;
    public ulong SteamId => 76561198414397975;
    public SAM.API.Client? Client => null;
    public SAM.API.Wrappers.SteamUserStats013? SteamUserStats => null;
    public SAM.API.Wrappers.SteamApps008? SteamApps008 => null;

    public void AddGame(GameModel game)
    {
        _games.Add(game);
    }

    public void SetGames(IEnumerable<GameModel> games)
    {
        _games.Clear();
        _games.AddRange(games);
    }

    public void Initialize(long appId = 0)
    {
        _isInitialized = true;
        _currentAppId = appId;
    }

    public void InitializeForGame(long gameId)
    {
        _isInitialized = true;
        _currentAppId = gameId;
    }

    public Task<IEnumerable<GameModel>> GetOwnedGamesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<GameModel>>(_games);
    }

    public string? GetGameName(uint appId)
    {
        return _games.FirstOrDefault(g => g.Id == appId)?.Name;
    }

    public string? GetCurrentGameLanguage()
    {
        return "english";
    }

    public bool OwnsGame(uint appId)
    {
        return _games.Any(g => g.Id == appId);
    }

    public string? GetGameLogoUrl(uint appId)
    {
        return _games.FirstOrDefault(g => g.Id == appId)?.ImageUrl;
    }

    public void RunCallbacks()
    {
        // No-op for testing
    }

    public void Dispose()
    {
        _isInitialized = false;
    }
}
