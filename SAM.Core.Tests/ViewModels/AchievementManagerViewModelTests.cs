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
using SAM.Core.ViewModels;

namespace SAM.Core.Tests.ViewModels;

public class AchievementManagerViewModelTests
{
    private readonly MockAchievementService _achievementService;
    private readonly MockImageCacheService _imageCacheService;
    private readonly MockUserDataService _userDataService;
    private readonly MockSteamService _steamService;
    private readonly AchievementManagerViewModel _viewModel;

    public AchievementManagerViewModelTests()
    {
        _achievementService = new MockAchievementService();
        _imageCacheService = new MockImageCacheService();
        _userDataService = new MockUserDataService();
        _steamService = new MockSteamService();
        _viewModel = new AchievementManagerViewModel(
            _achievementService, 
            _imageCacheService, 
            _userDataService,
            _steamService);
    }

    private static List<AchievementModel> CreateTestAchievements()
    {
        return
        [
            new() { Id = "ACH_1", Name = "First Blood", Description = "Get your first kill", IsUnlocked = false },
            new() { Id = "ACH_2", Name = "Veteran", Description = "Play 100 hours", IsUnlocked = true },
            new() { Id = "ACH_3", Name = "Master", Description = "Complete the game", IsUnlocked = false },
            new() { Id = "ACH_4", Name = "Hidden Achievement", Description = "Secret", IsUnlocked = false, IsHidden = true },
            new() { Id = "ACH_5", Name = "Protected Achievement", Description = "DRM Protected", IsUnlocked = false, IsProtected = true },
        ];
    }

    [Fact]
    public async Task InitializeAsync_LoadsAchievements()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);

        // Act
        await _viewModel.InitializeCommand.ExecuteAsync(440L);

        // Assert
        Assert.Equal(achievements.Count, _viewModel.Achievements.Count);
        Assert.Equal(440L, _viewModel.GameId);
    }

    [Fact]
    public async Task InitializeAsync_CalculatesUnlockedCount()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);

        // Act
        await _viewModel.InitializeCommand.ExecuteAsync(440L);

        // Assert
        Assert.Equal(1, _viewModel.UnlockedCount); // Only ACH_2 is unlocked
    }

    [Fact]
    public async Task InitializeAsync_CalculatesProtectedCount()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);

        // Act
        await _viewModel.InitializeCommand.ExecuteAsync(440L);

        // Assert
        Assert.Equal(1, _viewModel.ProtectedCount); // Only ACH_5 is protected
        Assert.True(_viewModel.HasProtectedAchievements);
    }

    [Fact]
    public async Task SearchText_FiltersAchievementsByName()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);
        await _viewModel.InitializeCommand.ExecuteAsync(440L);

        // Act
        _viewModel.SearchText = "Veteran";

        // Assert
        Assert.Single(_viewModel.FilteredAchievements);
        Assert.Equal("Veteran", _viewModel.FilteredAchievements[0].Name);
    }

    [Fact]
    public async Task SearchText_FiltersAchievementsByDescription()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);
        await _viewModel.InitializeCommand.ExecuteAsync(440L);

        // Act
        _viewModel.SearchText = "100 hours";

        // Assert
        Assert.Single(_viewModel.FilteredAchievements);
        Assert.Equal("Veteran", _viewModel.FilteredAchievements[0].Name);
    }

    [Fact]
    public async Task ToggleAchievement_UnlocksLockedAchievement()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);
        await _viewModel.InitializeCommand.ExecuteAsync(440L);
        var achievement = _viewModel.Achievements.First(a => a.Id == "ACH_1");

        // Act
        _viewModel.ToggleAchievementCommand.Execute(achievement);

        // Assert
        Assert.True(achievement.IsUnlocked);
        Assert.True(achievement.IsModified);
        Assert.True(_viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task ToggleAchievement_LocksUnlockedAchievement()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);
        await _viewModel.InitializeCommand.ExecuteAsync(440L);
        var achievement = _viewModel.Achievements.First(a => a.Id == "ACH_2");
        Assert.True(achievement.IsUnlocked); // Precondition

        // Act
        _viewModel.ToggleAchievementCommand.Execute(achievement);

        // Assert
        Assert.False(achievement.IsUnlocked);
        Assert.True(achievement.IsModified);
        Assert.True(_viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task ToggleAchievement_DoesNotModifyProtectedAchievement()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);
        await _viewModel.InitializeCommand.ExecuteAsync(440L);
        var protectedAchievement = _viewModel.Achievements.First(a => a.Id == "ACH_5");

        // Act
        _viewModel.ToggleAchievementCommand.Execute(protectedAchievement);

        // Assert
        Assert.False(protectedAchievement.IsUnlocked); // Should remain unchanged
        Assert.False(protectedAchievement.IsModified);
    }

    [Fact]
    public async Task UnlockAll_UnlocksAllUnprotectedAchievements()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        _achievementService.SetAchievements(achievements);
        await _viewModel.InitializeCommand.ExecuteAsync(440L);

        // Act
        _viewModel.UnlockAllCommand.Execute(null);

        // Assert: All except protected should be unlocked
        Assert.Equal(4, _viewModel.Achievements.Count(a => a.IsUnlocked));
        Assert.False(_viewModel.Achievements.First(a => a.Id == "ACH_5").IsUnlocked); // Protected remains locked
        Assert.True(_viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task LockAll_LocksAllUnprotectedAchievements()
    {
        // Arrange
        var achievements = CreateTestAchievements();
        // Unlock all first
        foreach (var a in achievements.Where(a => !a.IsProtected))
        {
            a.IsUnlocked = true;
        }
        _achievementService.SetAchievements(achievements);
        await _viewModel.InitializeCommand.ExecuteAsync(440L);

        // Act
        _viewModel.LockAllCommand.Execute(null);

        // Assert
        Assert.Equal(0, _viewModel.Achievements.Count(a => a.IsUnlocked && !a.IsProtected));
        Assert.True(_viewModel.HasUnsavedChanges);
    }

    [Fact]
    public void HeaderImageUrl_GeneratesCorrectUrl()
    {
        // Act
        _viewModel.GameId = 440;

        // Assert
        Assert.Equal("https://steamcdn-a.akamaihd.net/steam/apps/440/header.jpg", _viewModel.HeaderImageUrl);
    }

    [Fact]
    public void HeaderImageUrl_ReturnsEmptyForZeroGameId()
    {
        // Act
        _viewModel.GameId = 0;

        // Assert
        Assert.Equal(string.Empty, _viewModel.HeaderImageUrl);
    }
}
