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

namespace SAM.Core.Utilities;

/// <summary>
/// Centralized path management for SAM application data.
/// Structure:
/// %localappdata%/SAM/
/// ├── Logs/
/// ├── Userdata/{SteamID}/{GameID}/
/// ├── Cache/Cover/
/// ├── Cache/Widecover/
/// └── settings.json
/// </summary>
public static class AppPaths
{
    private static readonly Lazy<string> _basePath = new(() =>
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SAM");
        Directory.CreateDirectory(path);
        return path;
    });

    private static readonly Lazy<string> _logsPath = new(() =>
    {
        var path = Path.Combine(BasePath, "Logs");
        Directory.CreateDirectory(path);
        return path;
    });

    private static readonly Lazy<string> _userdataPath = new(() =>
    {
        var path = Path.Combine(BasePath, "Userdata");
        Directory.CreateDirectory(path);
        return path;
    });

    private static readonly Lazy<string> _cachePath = new(() =>
    {
        var path = Path.Combine(BasePath, "Cache");
        Directory.CreateDirectory(path);
        return path;
    });

    private static readonly Lazy<string> _coverCachePath = new(() =>
    {
        var path = Path.Combine(CachePath, "Cover");
        Directory.CreateDirectory(path);
        return path;
    });

    private static readonly Lazy<string> _widecoverCachePath = new(() =>
    {
        var path = Path.Combine(CachePath, "Widecover");
        Directory.CreateDirectory(path);
        return path;
    });

    private static readonly Lazy<string> _databasePath = new(() =>
    {
        return Path.Combine(BasePath, "games.db");
    });

    /// <summary>
    /// Gets the base SAM application data path (%localappdata%/SAM/).
    /// </summary>
    public static string BasePath => _basePath.Value;

    /// <summary>
    /// Gets the logs directory path (%localappdata%/SAM/Logs/).
    /// </summary>
    public static string LogsPath => _logsPath.Value;

    /// <summary>
    /// Gets the userdata directory path (%localappdata%/SAM/Userdata/).
    /// </summary>
    public static string UserdataPath => _userdataPath.Value;

    /// <summary>
    /// Gets the cache directory path (%localappdata%/SAM/Cache/).
    /// </summary>
    public static string CachePath => _cachePath.Value;

    /// <summary>
    /// Gets the cover image cache path (%localappdata%/SAM/Cache/Cover/).
    /// </summary>
    public static string CoverCachePath => _coverCachePath.Value;

    /// <summary>
    /// Gets the wide cover image cache path (%localappdata%/SAM/Cache/Widecover/).
    /// </summary>
    public static string WidecoverCachePath => _widecoverCachePath.Value;

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public static string SettingsFilePath => Path.Combine(BasePath, "settings.json");

    /// <summary>
    /// Gets the SQLite database file path (%localappdata%/SAM/games.db).
    /// </summary>
    public static string DatabasePath => _databasePath.Value;

    /// <summary>
    /// Gets the userdata path for a specific Steam user.
    /// </summary>
    /// <param name="steamId">The Steam ID or username.</param>
    /// <returns>Path to the user's data directory.</returns>
    public static string GetUserPath(string steamId)
    {
        var sanitized = SanitizeFileName(steamId);
        var path = Path.Combine(UserdataPath, sanitized);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Gets the game data path for a specific user and game.
    /// </summary>
    /// <param name="steamId">The Steam ID or username.</param>
    /// <param name="gameId">The game's App ID.</param>
    /// <returns>Path to the game's data directory.</returns>
    public static string GetGamePath(string steamId, uint gameId)
    {
        var userPath = GetUserPath(steamId);
        var path = Path.Combine(userPath, gameId.ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Gets the game data file path for a specific user and game.
    /// </summary>
    /// <param name="steamId">The Steam ID or username.</param>
    /// <param name="gameId">The game's App ID.</param>
    /// <returns>Path to the game's data JSON file.</returns>
    public static string GetGameDataFilePath(string steamId, uint gameId)
    {
        return Path.Combine(GetGamePath(steamId, gameId), "gamedata.json");
    }

    /// <summary>
    /// Gets all recognized Steam user directories.
    /// </summary>
    /// <returns>List of Steam ID/username directories.</returns>
    public static IEnumerable<string> GetAllUsers()
    {
        if (!Directory.Exists(UserdataPath))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetDirectories(UserdataPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))!;
    }

    /// <summary>
    /// Gets all game directories for a specific user.
    /// </summary>
    /// <param name="steamId">The Steam ID or username.</param>
    /// <returns>List of game IDs as strings.</returns>
    public static IEnumerable<string> GetUserGames(string steamId)
    {
        var userPath = Path.Combine(UserdataPath, SanitizeFileName(steamId));
        if (!Directory.Exists(userPath))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetDirectories(userPath)
            .Select(Path.GetFileName)
            .Where(name => uint.TryParse(name, out _))!;
    }

    /// <summary>
    /// Migrates old log files from root to Logs subdirectory.
    /// </summary>
    public static void MigrateLegacyLogs()
    {
        try
        {
            var legacyLogs = Directory.GetFiles(BasePath, "sam_*.log");
            foreach (var logFile in legacyLogs)
            {
                var fileName = Path.GetFileName(logFile);
                var newPath = Path.Combine(LogsPath, fileName);
                if (!File.Exists(newPath))
                {
                    File.Move(logFile, newPath);
                }
                else
                {
                    File.Delete(logFile);
                }
            }
        }
        catch
        {
            // Ignore migration errors
        }
    }

    /// <summary>
    /// Migrates old ImageCache folder to Cache/Cover.
    /// </summary>
    public static void MigrateLegacyImageCache()
    {
        try
        {
            var legacyCache = Path.Combine(BasePath, "ImageCache");
            if (Directory.Exists(legacyCache))
            {
                var files = Directory.GetFiles(legacyCache);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var newPath = Path.Combine(CoverCachePath, fileName);
                    if (!File.Exists(newPath))
                    {
                        File.Move(file, newPath);
                    }
                }
                
                // Remove old directory if empty
                if (!Directory.EnumerateFileSystemEntries(legacyCache).Any())
                {
                    Directory.Delete(legacyCache);
                }
            }
        }
        catch
        {
            // Ignore migration errors
        }
    }

    /// <summary>
    /// Cleans up old log files older than the specified time span.
    /// </summary>
    /// <param name="maxAge">Maximum age (default: 10 minutes).</param>
    public static void CleanupOldLogs(TimeSpan? maxAge = null)
    {
        try
        {
            var age = maxAge ?? TimeSpan.FromMinutes(10);
            var cutoff = DateTime.Now - age;
            var oldLogs = Directory.GetFiles(LogsPath, "*.log")
                .Where(f => File.GetCreationTime(f) < cutoff);
            
            foreach (var logFile in oldLogs)
            {
                File.Delete(logFile);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
