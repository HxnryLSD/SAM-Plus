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
using SAM.Core.Tests.Mocks;
using SAM.Core.ViewModels;

namespace SAM.Core.Tests.ViewModels;

public class GamePickerViewModelTests
{
    private readonly MockSteamService _steamService;
    private readonly MockImageCacheService _imageCacheService;
    private readonly MockUserDataService _userDataService;
    private readonly MockGameCacheService _gameCacheService;
    private readonly GamePickerViewModel _viewModel;

    public GamePickerViewModelTests()
    {
        _steamService = new MockSteamService();
        _imageCacheService = new MockImageCacheService();
        _userDataService = new MockUserDataService();
        _gameCacheService = new MockGameCacheService();
        _viewModel = new GamePickerViewModel(
            () => _steamService,
            () => _imageCacheService,
            () => _userDataService,
            () => _gameCacheService);
    }

    private static List<GameModel> CreateTestGames()
    {
        return
        [
            new() { Id = 440, Name = "Team Fortress 2", Type = GameType.Game, AchievementCount = 520 },
            new() { Id = 730, Name = "Counter-Strike 2", Type = GameType.Game, AchievementCount = 167 },
            new() { Id = 570, Name = "Dota 2", Type = GameType.Game, AchievementCount = 0 },
            new() { Id = 1001, Name = "Test Mod", Type = GameType.Mod, AchievementCount = 10 },
            new() { Id = 1002, Name = "Test DLC", Type = GameType.Dlc, AchievementCount = 5 },
            new() { Id = 1003, Name = "Test Demo", Type = GameType.Demo, AchievementCount = 3 },
        ];
    }

    [Fact]
    public async Task LoadGamesAsync_LoadsGamesFromService()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);

        // Act
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(testGames.Count, _viewModel.Games.Count);
        Assert.Equal(testGames.Count, _viewModel.FilteredGames.Count);
    }

    [Fact]
    public async Task TryLoadCachedGamesAsync_LoadsCachedGames()
    {
        // Arrange
        var cached = new CachedGameInfo
        {
            AppId = 440,
            Name = "Team Fortress 2",
            AchievementCount = 520,
            UnlockedCount = 120,
            HasDrm = false,
            ImageUrl = "https://example.com/440.jpg",
            LastUpdated = DateTime.UtcNow,
            LastPlayed = null,
            PlaytimeMinutes = 123
        };
        _gameCacheService.SetGame(cached);

        // Act
        var loaded = await _viewModel.TryLoadCachedGamesAsync();

        // Assert
        Assert.True(loaded);
        Assert.Single(_viewModel.Games);
        Assert.Equal(cached.AppId, _viewModel.Games[0].Id);
        Assert.Equal(cached.Name, _viewModel.Games[0].Name);
    }

    [Fact]
    public async Task LoadGamesAsync_InitializesSteamIfNeeded()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);

        // Act
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_steamService.IsInitialized);
    }

    [Fact]
    public async Task SearchText_FiltersGamesByName()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act
        _viewModel.SearchText = "Counter";

        // Assert
        Assert.Single(_viewModel.FilteredGames);
        Assert.Equal("Counter-Strike 2", _viewModel.FilteredGames[0].Name);
    }

    [Fact]
    public async Task SearchText_FiltersGamesByAppId()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act
        _viewModel.SearchText = "440";

        // Assert
        Assert.Single(_viewModel.FilteredGames);
        Assert.Equal((uint)440, _viewModel.FilteredGames[0].Id);
    }

    [Fact]
    public async Task SearchText_CaseInsensitive()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act
        _viewModel.SearchText = "TEAM FORTRESS";

        // Assert
        Assert.Single(_viewModel.FilteredGames);
        Assert.Equal("Team Fortress 2", _viewModel.FilteredGames[0].Name);
    }

    [Fact]
    public async Task FilterType_GamesOnly_ShowsOnlyGames()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterType = GameFilterType.GamesOnly;

        // Assert
        Assert.Equal(3, _viewModel.FilteredGames.Count);
        Assert.All(_viewModel.FilteredGames, g => Assert.Equal(GameType.Game, g.Type));
    }

    [Fact]
    public async Task FilterType_ModsOnly_ShowsOnlyMods()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterType = GameFilterType.ModsOnly;

        // Assert
        Assert.Single(_viewModel.FilteredGames);
        Assert.Equal(GameType.Mod, _viewModel.FilteredGames[0].Type);
    }

    [Fact]
    public async Task FilterType_DlcOnly_ShowsOnlyDlc()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterType = GameFilterType.DlcOnly;

        // Assert
        Assert.Single(_viewModel.FilteredGames);
        Assert.Equal(GameType.Dlc, _viewModel.FilteredGames[0].Type);
    }

    [Fact]
    public async Task FilterType_DemosOnly_ShowsOnlyDemos()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterType = GameFilterType.DemosOnly;

        // Assert
        Assert.Single(_viewModel.FilteredGames);
        Assert.Equal(GameType.Demo, _viewModel.FilteredGames[0].Type);
    }

    [Fact]
    public async Task FilterType_All_ShowsAllGames()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // First apply a filter
        _viewModel.FilterType = GameFilterType.GamesOnly;
        
        // Act
        _viewModel.FilterType = GameFilterType.All;

        // Assert
        Assert.Equal(testGames.Count, _viewModel.FilteredGames.Count);
    }

    [Fact]
    public async Task SearchAndFilter_CombineCorrectly()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Act: Search for "Test" and filter by Mods
        _viewModel.SearchText = "Test";
        _viewModel.FilterType = GameFilterType.ModsOnly;

        // Assert
        Assert.Single(_viewModel.FilteredGames);
        Assert.Equal("Test Mod", _viewModel.FilteredGames[0].Name);
    }

    [Fact]
    public void SelectGame_SetsSelectedGame()
    {
        // Arrange
        var game = new GameModel { Id = 440, Name = "Team Fortress 2" };

        // Act
        _viewModel.SelectGameCommand.Execute(game);

        // Assert
        Assert.Equal(game, _viewModel.SelectedGame);
    }

    [Fact]
    public void ClearSearch_ResetsSearchText()
    {
        // Arrange
        _viewModel.SearchText = "some search";

        // Act
        _viewModel.ClearSearchCommand.Execute(null);

        // Assert
        Assert.Equal(string.Empty, _viewModel.SearchText);
    }

    [Fact]
    public async Task RefreshGamesAsync_ClearsAndReloadsGames()
    {
        // Arrange
        var initialGames = CreateTestGames();
        _steamService.SetGames(initialGames);
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);
        
        // Change the games
        var newGames = new List<GameModel>
        {
            new() { Id = 999, Name = "New Game", Type = GameType.Game, AchievementCount = 50 }
        };
        _steamService.SetGames(newGames);

        // Act
        await _viewModel.RefreshGamesCommand.ExecuteAsync(null);

        // Assert
        Assert.Single(_viewModel.Games);
        Assert.Equal("New Game", _viewModel.Games[0].Name);
    }

    [Fact]
    public async Task LoadGamesAsync_MergesUserDataWithGames()
    {
        // Arrange
        var testGames = new List<GameModel>
        {
            new() { Id = 440, Name = "Team Fortress 2", Type = GameType.Game, AchievementCount = 520 },
            new() { Id = 730, Name = "Counter-Strike 2", Type = GameType.Game, AchievementCount = 167 },
        };
        _steamService.SetGames(testGames);
        
        // Save user data with DRM protection info for one game
        await _userDataService.SaveGameDataAsync(new Models.GameUserData
        {
            GameId = 440,
            GameName = "Team Fortress 2",
            HasDrmProtection = true,
            ProtectedAchievementCount = 5,
            DrmProtectionInfo = "Server-side validated achievements",
            TotalAchievementCount = 520,
            UnlockedAchievementCount = 100,
            IsInitialized = true
        });

        // Act
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Assert - DRM info should be merged
        var tf2Game = _viewModel.Games.FirstOrDefault(g => g.Id == 440);
        Assert.NotNull(tf2Game);
        Assert.True(tf2Game.HasDrmProtection);
        Assert.Equal(5, tf2Game.ProtectedAchievementCount);
        Assert.Equal("Server-side validated achievements", tf2Game.DrmProtectionInfo);
        Assert.Equal(520, tf2Game.AchievementCount);
        Assert.Equal(100, tf2Game.UnlockedAchievementCount);
        
        // CS2 should not have DRM info
        var cs2Game = _viewModel.Games.FirstOrDefault(g => g.Id == 730);
        Assert.NotNull(cs2Game);
        Assert.False(cs2Game.HasDrmProtection);
    }

    [Fact]
    public async Task LoadGamesAsync_SetsCurrentUserInfo()
    {
        // Arrange
        var testGames = CreateTestGames();
        _steamService.SetGames(testGames);

        // Act
        await _viewModel.LoadGamesCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(_viewModel.CurrentUserInfo);
        Assert.Equal(_steamService.SteamId.ToString(), _viewModel.CurrentUserInfo);
    }
}
