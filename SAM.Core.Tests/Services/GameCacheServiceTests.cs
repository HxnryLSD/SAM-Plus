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

using SAM.Core.Services;

namespace SAM.Core.Tests.Services;

public class GameCacheServiceTests
{
    [Fact]
    public async Task GetAllGamesAsync_WhenEmpty_ReturnsEmptyList()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        var result = await service.GetAllGamesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveGameAsync_ThenGetGameAsync_RoundTrips()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        var game = CreateGame(440, "Team Fortress 2", achievementCount: 10, unlockedCount: 4, hasDrm: true);
        var lastPlayed = DateTime.UtcNow.AddDays(-1);
        game = game with { ImageUrl = "https://example.com/tf2.jpg", LastPlayed = lastPlayed, PlaytimeMinutes = 120 };

        await service.SaveGameAsync(game);
        var result = await service.GetGameAsync(440);

        Assert.NotNull(result);
        Assert.Equal(game.AppId, result!.AppId);
        Assert.Equal(game.Name, result.Name);
        Assert.Equal(game.AchievementCount, result.AchievementCount);
        Assert.Equal(game.UnlockedCount, result.UnlockedCount);
        Assert.True(result.HasDrm);
        Assert.Equal(game.ImageUrl, result.ImageUrl);
        Assert.NotNull(result.LastPlayed);
        Assert.Equal(lastPlayed.ToUniversalTime(), result.LastPlayed!.Value.ToUniversalTime());
        Assert.Equal(game.PlaytimeMinutes, result.PlaytimeMinutes);
    }

    [Fact]
    public async Task SaveGamesAsync_BatchInsert_WritesAll()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        var games = new[]
        {
            CreateGame(10, "Counter-Strike"),
            CreateGame(20, "Half-Life")
        };

        await service.SaveGamesAsync(games);
        var result = await service.GetAllGamesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, g => g.AppId == 10);
        Assert.Contains(result, g => g.AppId == 20);
    }

    [Fact]
    public async Task GetGamesForUserAsync_ReturnsOnlyLinkedGames()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        var game1 = CreateGame(30, "Portal");
        var game2 = CreateGame(40, "Portal 2");
        var game3 = CreateGame(50, "Half-Life 2");

        await service.SaveGameAsync(game1, "steam-1");
        await service.SaveGameAsync(game2, "steam-1");
        await service.SaveGameAsync(game3, "steam-2");

        var result = await service.GetGamesForUserAsync("steam-1");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, g => g.AppId == 30);
        Assert.Contains(result, g => g.AppId == 40);
        Assert.DoesNotContain(result, g => g.AppId == 50);
    }

    [Fact]
    public async Task SearchGamesAsync_SubstringMatch_ReturnsMatches()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        await service.SaveGamesAsync([
            CreateGame(60, "Portal"),
            CreateGame(70, "Portal 2"),
            CreateGame(80, "Half-Life")
        ]);

        var result = await service.SearchGamesAsync("Portal");

        Assert.Equal(2, result.Count);
        Assert.All(result, g => Assert.Contains("Portal", g.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateAchievementCountsAsync_UpdatesCounts()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        await service.SaveGameAsync(CreateGame(90, "Left 4 Dead", achievementCount: 1, unlockedCount: 0));
        await service.UpdateAchievementCountsAsync(90, total: 5, unlocked: 3);

        var result = await service.GetGameAsync(90);

        Assert.NotNull(result);
        Assert.Equal(5, result!.AchievementCount);
        Assert.Equal(3, result.UnlockedCount);
    }

    [Fact]
    public async Task RemoveGameAsync_RemovesGame()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        await service.SaveGameAsync(CreateGame(100, "Dota 2"));
        await service.RemoveGameAsync(100);

        var result = await service.GetGameAsync(100);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearCacheAsync_RemovesAllGames()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        await service.SaveGamesAsync([
            CreateGame(110, "Game A"),
            CreateGame(120, "Game B")
        ]);

        await service.ClearCacheAsync();
        var result = await service.GetAllGamesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsTotalsAndDates()
    {
        using var tempDb = new TempDatabase();
        using var service = tempDb.CreateService();

        var older = CreateGame(130, "Game Older") with { LastUpdated = DateTime.UtcNow.AddDays(-2) };
        var newer = CreateGame(140, "Game Newer") with { LastUpdated = DateTime.UtcNow.AddDays(-1) };

        await service.SaveGamesAsync([older, newer]);
        var stats = await service.GetStatisticsAsync();

        Assert.Equal(2, stats.TotalGames);
        Assert.NotNull(stats.OldestEntry);
        Assert.NotNull(stats.NewestEntry);
        Assert.True(stats.DatabaseSizeBytes >= 0);
    }

    private static CachedGameInfo CreateGame(uint appId, string name, int achievementCount = 0, int unlockedCount = 0, bool hasDrm = false)
    {
        return new CachedGameInfo
        {
            AppId = appId,
            Name = name,
            AchievementCount = achievementCount,
            UnlockedCount = unlockedCount,
            HasDrm = hasDrm,
            ImageUrl = null,
            LastUpdated = DateTime.UtcNow,
            LastPlayed = null,
            PlaytimeMinutes = 0
        };
    }

    private sealed class TempDatabase : IDisposable
    {
        private readonly string _connectionString;

        public TempDatabase()
        {
            var id = Guid.NewGuid().ToString("N");
            _connectionString = $"Data Source=file:gamecache_{id}?mode=memory&cache=shared";
        }

        public GameCacheService CreateService()
        {
            return new GameCacheService(_connectionString, databasePath: null, keepAliveConnection: true);
        }

        public void Dispose()
        {
            // In-memory database; nothing to clean up on disk.
        }
    }
}
