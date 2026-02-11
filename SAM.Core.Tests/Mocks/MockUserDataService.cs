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
/// Mock implementation of IUserDataService for testing.
/// </summary>
public class MockUserDataService : IUserDataService
{
    private readonly Dictionary<uint, GameUserData> _gameData = new();
    private readonly HashSet<string> _users = new() { "testuser123" };
    private string? _currentUserId = "testuser123";

    public string? CurrentUserId => _currentUserId;

    public void SetCurrentUser(string steamId)
    {
        _currentUserId = steamId;
        _users.Add(steamId);
    }

    public Task<GameUserData?> GetGameDataAsync(uint gameId, CancellationToken cancellationToken = default)
    {
        _gameData.TryGetValue(gameId, out var data);
        return Task.FromResult(data);
    }

    public Task SaveGameDataAsync(GameUserData data, CancellationToken cancellationToken = default)
    {
        _gameData[data.GameId] = data;
        return Task.CompletedTask;
    }

    public Task<Dictionary<uint, GameUserData>> GetAllGameDataAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Dictionary<uint, GameUserData>(_gameData));
    }

    public Task DeleteGameDataAsync(uint gameId)
    {
        _gameData.Remove(gameId);
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetAllUsers()
    {
        return _users;
    }
}
