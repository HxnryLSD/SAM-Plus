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
using SAM.Core.Tests.Mocks;

namespace SAM.Core.Tests.Services;

public class UserDataServiceTests
{
    [Fact]
    public void CurrentUserId_ReturnsDefault()
    {
        // Arrange
        var service = new MockUserDataService();

        // Act & Assert
        Assert.Equal("testuser123", service.CurrentUserId);
    }

    [Fact]
    public void SetCurrentUser_UpdatesCurrentUserId()
    {
        // Arrange
        var service = new MockUserDataService();

        // Act
        service.SetCurrentUser("newuser456");

        // Assert
        Assert.Equal("newuser456", service.CurrentUserId);
    }

    [Fact]
    public async Task GetGameDataAsync_ReturnsNullForNewGame()
    {
        // Arrange
        var service = new MockUserDataService();

        // Act
        var result = await service.GetGameDataAsync(440);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveGameDataAsync_StoresAndRetrievesData()
    {
        // Arrange
        var service = new MockUserDataService();
        var data = new GameUserData { GameId = 440 };

        // Act
        await service.SaveGameDataAsync(data);
        var result = await service.GetGameDataAsync(440);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(440u, result.GameId);
    }

    [Fact]
    public async Task GetAllGameDataAsync_ReturnsAllStoredData()
    {
        // Arrange
        var service = new MockUserDataService();
        await service.SaveGameDataAsync(new GameUserData { GameId = 440 });
        await service.SaveGameDataAsync(new GameUserData { GameId = 730 });

        // Act
        var result = await service.GetAllGameDataAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(440));
        Assert.True(result.ContainsKey(730));
    }

    [Fact]
    public async Task DeleteGameDataAsync_RemovesData()
    {
        // Arrange
        var service = new MockUserDataService();
        await service.SaveGameDataAsync(new GameUserData { GameId = 440 });

        // Act
        await service.DeleteGameDataAsync(440);
        var result = await service.GetGameDataAsync(440);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllUsers_ReturnsUsers()
    {
        // Arrange
        var service = new MockUserDataService();
        service.SetCurrentUser("user1");
        service.SetCurrentUser("user2");

        // Act
        var result = service.GetAllUsers().ToList();

        // Assert
        Assert.Contains("testuser123", result);
        Assert.Contains("user1", result);
        Assert.Contains("user2", result);
    }
}
