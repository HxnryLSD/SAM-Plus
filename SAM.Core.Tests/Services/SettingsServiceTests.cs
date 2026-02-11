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

using SAM.Core.Tests.Mocks;

namespace SAM.Core.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void Theme_DefaultValue_IsSystem()
    {
        // Arrange
        var service = new MockSettingsService();

        // Assert
        Assert.Equal("System", service.Theme);
    }

    [Fact]
    public void Theme_CanBeSet()
    {
        // Arrange
        var service = new MockSettingsService();

        // Act
        service.Theme = "Dark";

        // Assert
        Assert.Equal("Dark", service.Theme);
    }

    [Fact]
    public void Language_DefaultValue_IsEmpty()
    {
        // Arrange
        var service = new MockSettingsService();

        // Assert
        Assert.Equal("", service.Language);
    }

    [Fact]
    public void Language_CanBeSet()
    {
        // Arrange
        var service = new MockSettingsService();

        // Act
        service.Language = "de";

        // Assert
        Assert.Equal("de", service.Language);
    }

    [Fact]
    public void WarnOnUnsavedChanges_DefaultValue_IsTrue()
    {
        // Arrange
        var service = new MockSettingsService();

        // Assert
        Assert.True(service.WarnOnUnsavedChanges);
    }

    [Fact]
    public void ShowHiddenAchievements_DefaultValue_IsTrue()
    {
        // Arrange
        var service = new MockSettingsService();

        // Assert
        Assert.True(service.ShowHiddenAchievements);
    }

    [Fact]
    public void ShowOnlyGamesWithAchievements_DefaultValue_IsFalse()
    {
        // Arrange
        var service = new MockSettingsService();

        // Assert
        Assert.False(service.ShowOnlyGamesWithAchievements);
    }

    [Fact]
    public void DefaultGameFilter_DefaultValue_IsZero()
    {
        // Arrange
        var service = new MockSettingsService();

        // Assert
        Assert.Equal(0, service.DefaultGameFilter);
    }

    [Fact]
    public async Task LoadAsync_SetsLoadAsyncCalled()
    {
        // Arrange
        var service = new MockSettingsService();

        // Act
        await service.LoadAsync();

        // Assert
        Assert.True(service.LoadAsyncCalled);
    }

    [Fact]
    public async Task SaveAsync_SetsSaveAsyncCalled()
    {
        // Arrange
        var service = new MockSettingsService();

        // Act
        await service.SaveAsync();

        // Assert
        Assert.True(service.SaveAsyncCalled);
    }

    [Fact]
    public void ResetToDefaults_ResetsAllValues()
    {
        // Arrange
        var service = new MockSettingsService();
        service.Theme = "Dark";
        service.Language = "de";
        service.ShowOnlyGamesWithAchievements = true;
        service.WarnOnUnsavedChanges = false;
        service.ShowHiddenAchievements = false;

        // Act
        service.ResetToDefaults();

        // Assert
        Assert.True(service.ResetCalled);
        Assert.Equal("System", service.Theme);
        Assert.Equal("", service.Language);
        Assert.False(service.ShowOnlyGamesWithAchievements);
        Assert.True(service.WarnOnUnsavedChanges);
        Assert.True(service.ShowHiddenAchievements);
    }
}
