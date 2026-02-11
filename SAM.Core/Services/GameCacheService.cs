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

using Microsoft.Data.Sqlite;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// SQLite-based game metadata cache service.
/// Provides persistent storage for game information for fast offline access.
/// </summary>
public class GameCacheService : IGameCacheService, IDisposable
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _initialized;
    private bool _disposed;

    public GameCacheService()
    {
        _connectionString = $"Data Source={AppPaths.DatabasePath}";
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS Games (
                    AppId INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    AchievementCount INTEGER DEFAULT 0,
                    UnlockedCount INTEGER DEFAULT 0,
                    HasDrm INTEGER DEFAULT 0,
                    ImageUrl TEXT,
                    LastUpdated TEXT NOT NULL,
                    LastPlayed TEXT,
                    PlaytimeMinutes INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS UserGames (
                    SteamId TEXT NOT NULL,
                    AppId INTEGER NOT NULL,
                    PRIMARY KEY (SteamId, AppId),
                    FOREIGN KEY (AppId) REFERENCES Games(AppId) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_games_name ON Games(Name);
                CREATE INDEX IF NOT EXISTS idx_usergames_steamid ON UserGames(SteamId);
            ";

            await using var command = new SqliteCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            _initialized = true;
            Log.Debug("GameCacheService: SQLite database initialized");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CachedGameInfo?> GetGameAsync(uint appId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM Games WHERE AppId = @AppId";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@AppId", appId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return ReadGameFromReader(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<CachedGameInfo>> GetAllGamesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var games = new List<CachedGameInfo>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM Games ORDER BY Name";
        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            games.Add(ReadGameFromReader(reader));
        }

        return games;
    }

    public async Task<IReadOnlyList<CachedGameInfo>> GetGamesForUserAsync(string steamId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var games = new List<CachedGameInfo>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT g.* FROM Games g
            INNER JOIN UserGames ug ON g.AppId = ug.AppId
            WHERE ug.SteamId = @SteamId
            ORDER BY g.Name";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@SteamId", steamId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            games.Add(ReadGameFromReader(reader));
        }

        return games;
    }

    public async Task SaveGameAsync(CachedGameInfo game, string? steamId = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await InsertOrUpdateGameAsync(connection, game, cancellationToken);

            if (!string.IsNullOrEmpty(steamId))
            {
                await LinkGameToUserAsync(connection, steamId, game.AppId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SaveGamesAsync(IEnumerable<CachedGameInfo> games, string? steamId = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var game in games)
            {
                await InsertOrUpdateGameAsync(connection, game, cancellationToken);

                if (!string.IsNullOrEmpty(steamId))
                {
                    await LinkGameToUserAsync(connection, steamId, game.AppId, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            Log.Debug($"GameCacheService: Saved batch of games to cache");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task InsertOrUpdateGameAsync(SqliteConnection connection, CachedGameInfo game, CancellationToken cancellationToken)
    {
        var sql = @"
            INSERT INTO Games (AppId, Name, AchievementCount, UnlockedCount, HasDrm, ImageUrl, LastUpdated, LastPlayed, PlaytimeMinutes)
            VALUES (@AppId, @Name, @AchievementCount, @UnlockedCount, @HasDrm, @ImageUrl, @LastUpdated, @LastPlayed, @PlaytimeMinutes)
            ON CONFLICT(AppId) DO UPDATE SET
                Name = excluded.Name,
                AchievementCount = excluded.AchievementCount,
                UnlockedCount = excluded.UnlockedCount,
                HasDrm = excluded.HasDrm,
                ImageUrl = COALESCE(excluded.ImageUrl, ImageUrl),
                LastUpdated = excluded.LastUpdated,
                LastPlayed = COALESCE(excluded.LastPlayed, LastPlayed),
                PlaytimeMinutes = CASE WHEN excluded.PlaytimeMinutes > 0 THEN excluded.PlaytimeMinutes ELSE PlaytimeMinutes END";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@AppId", game.AppId);
        command.Parameters.AddWithValue("@Name", game.Name);
        command.Parameters.AddWithValue("@AchievementCount", game.AchievementCount);
        command.Parameters.AddWithValue("@UnlockedCount", game.UnlockedCount);
        command.Parameters.AddWithValue("@HasDrm", game.HasDrm ? 1 : 0);
        command.Parameters.AddWithValue("@ImageUrl", game.ImageUrl ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LastUpdated", game.LastUpdated.ToString("O"));
        command.Parameters.AddWithValue("@LastPlayed", game.LastPlayed?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PlaytimeMinutes", game.PlaytimeMinutes);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task LinkGameToUserAsync(SqliteConnection connection, string steamId, uint appId, CancellationToken cancellationToken)
    {
        var sql = "INSERT OR IGNORE INTO UserGames (SteamId, AppId) VALUES (@SteamId, @AppId)";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@SteamId", steamId);
        command.Parameters.AddWithValue("@AppId", appId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAchievementCountsAsync(uint appId, int total, int unlocked, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE Games 
            SET AchievementCount = @Total, UnlockedCount = @Unlocked, LastUpdated = @LastUpdated
            WHERE AppId = @AppId";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@AppId", appId);
        command.Parameters.AddWithValue("@Total", total);
        command.Parameters.AddWithValue("@Unlocked", unlocked);
        command.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoveGameAsync(uint appId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = "DELETE FROM Games WHERE AppId = @AppId";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@AppId", appId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqliteCommand("DELETE FROM UserGames; DELETE FROM Games;", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        Log.Info("GameCacheService: Cache cleared");
    }

    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT 
                COUNT(*) as TotalGames,
                MIN(LastUpdated) as OldestEntry,
                MAX(LastUpdated) as NewestEntry
            FROM Games";

        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        int totalGames = 0;
        DateTime? oldest = null;
        DateTime? newest = null;

        if (await reader.ReadAsync(cancellationToken))
        {
            totalGames = reader.GetInt32(0);
            
            if (!reader.IsDBNull(1))
            {
                oldest = DateTime.Parse(reader.GetString(1));
            }
            if (!reader.IsDBNull(2))
            {
                newest = DateTime.Parse(reader.GetString(2));
            }
        }

        // Get database file size
        long dbSize = 0;
        if (File.Exists(AppPaths.DatabasePath))
        {
            dbSize = new FileInfo(AppPaths.DatabasePath).Length;
        }

        return new CacheStatistics
        {
            TotalGames = totalGames,
            DatabaseSizeBytes = dbSize,
            OldestEntry = oldest,
            NewestEntry = newest
        };
    }

    public async Task<IReadOnlyList<CachedGameInfo>> SearchGamesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllGamesAsync(cancellationToken);
        }

        await EnsureInitializedAsync(cancellationToken);

        var games = new List<CachedGameInfo>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM Games WHERE Name LIKE @Query ORDER BY Name";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Query", $"%{query}%");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            games.Add(ReadGameFromReader(reader));
        }

        return games;
    }

    private static CachedGameInfo ReadGameFromReader(SqliteDataReader reader)
    {
        return new CachedGameInfo
        {
            AppId = (uint)reader.GetInt64(reader.GetOrdinal("AppId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            AchievementCount = reader.GetInt32(reader.GetOrdinal("AchievementCount")),
            UnlockedCount = reader.GetInt32(reader.GetOrdinal("UnlockedCount")),
            HasDrm = reader.GetInt32(reader.GetOrdinal("HasDrm")) == 1,
            ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
            LastUpdated = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastUpdated"))),
            LastPlayed = reader.IsDBNull(reader.GetOrdinal("LastPlayed")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("LastPlayed"))),
            PlaytimeMinutes = reader.GetInt32(reader.GetOrdinal("PlaytimeMinutes"))
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lock.Dispose();
    }
}
