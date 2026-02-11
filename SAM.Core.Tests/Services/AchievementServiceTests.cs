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

public class AchievementServiceTests
{
    [Fact]
    public async Task InitializeAsync_SetsGameIdAndIsReady()
    {
        // Arrange
        var service = new MockAchievementService();

        // Act
        await service.InitializeAsync(440);

        // Assert
        Assert.Equal(440L, service.GameId);
        Assert.True(service.IsReady);
    }

    [Fact]
    public async Task GetAchievementsAsync_ReturnsSetAchievements()
    {
        // Arrange
        var service = new MockAchievementService();
        var achievements = new List<AchievementModel>
        {
            new() { Id = "ACH_1", Name = "Test 1" },
            new() { Id = "ACH_2", Name = "Test 2" }
        };
        service.SetAchievements(achievements);

        // Act
        var result = await service.GetAchievementsAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void SetAchievement_UnlocksAchievement()
    {
        // Arrange
        var service = new MockAchievementService();
        var achievements = new List<AchievementModel>
        {
            new() { Id = "ACH_1", Name = "Test", IsUnlocked = false }
        };
        service.SetAchievements(achievements);

        // Act
        var result = service.SetAchievement("ACH_1", true);

        // Assert
        Assert.True(result);
        Assert.True(achievements[0].IsUnlocked);
    }

    [Fact]
    public void SetAchievement_LocksAchievement()
    {
        // Arrange
        var service = new MockAchievementService();
        var achievements = new List<AchievementModel>
        {
            new() { Id = "ACH_1", Name = "Test", IsUnlocked = true }
        };
        service.SetAchievements(achievements);

        // Act
        var result = service.SetAchievement("ACH_1", false);

        // Assert
        Assert.True(result);
        Assert.False(achievements[0].IsUnlocked);
    }

    [Fact]
    public void SetAchievement_ReturnsFalseForUnknownId()
    {
        // Arrange
        var service = new MockAchievementService();
        service.SetAchievements([]);

        // Act
        var result = service.SetAchievement("UNKNOWN", true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SetStatistic_StoresValue()
    {
        // Arrange
        var service = new MockAchievementService();

        // Act
        var result = service.SetStatistic("kills", 100);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StoreStats_RaisesStatsReceivedEvent()
    {
        // Arrange
        var service = new MockAchievementService();
        bool eventRaised = false;
        service.StatsReceived += (s, e) => eventRaised = true;

        // Act
        var result = service.StoreStats();

        // Assert
        Assert.True(result);
        Assert.True(eventRaised);
    }

    [Fact]
    public void ResetAllStats_WithoutAchievements_ClearsOnlyStats()
    {
        // Arrange
        var service = new MockAchievementService();
        var achievements = new List<AchievementModel>
        {
            new() { Id = "ACH_1", Name = "Test", IsUnlocked = true }
        };
        service.SetAchievements(achievements);
        service.SetStatistic("kills", 100);

        // Act
        var result = service.ResetAllStats(includeAchievements: false);

        // Assert  
        Assert.True(result);
        Assert.True(achievements[0].IsUnlocked); // Achievement should remain unlocked
    }

    [Fact]
    public void ResetAllStats_WithAchievements_ClearsEverything()
    {
        // Arrange
        var service = new MockAchievementService();
        var achievements = new List<AchievementModel>
        {
            new() { Id = "ACH_1", Name = "Test", IsUnlocked = true }
        };
        service.SetAchievements(achievements);
        service.SetStatistic("kills", 100);

        // Act
        var result = service.ResetAllStats(includeAchievements: true);

        // Assert
        Assert.True(result);
        Assert.False(achievements[0].IsUnlocked); // Achievement should be locked
    }
}
