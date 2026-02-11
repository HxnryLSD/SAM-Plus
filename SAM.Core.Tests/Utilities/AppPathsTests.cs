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

using System.Reflection;
using SAM.Core.Utilities;

namespace SAM.Core.Tests.Utilities;

public class AppPathsTests
{
    [Fact]
    public void GetUserPath_ReturnsPathUnderUserdata()
    {
        var userId = "SAM.Tests.User:123?*";
        var expectedName = InvokeSanitizeFileName(userId);

        var path = AppPaths.GetUserPath(userId);

        Assert.True(Directory.Exists(path));
        Assert.StartsWith(AppPaths.UserdataPath, path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(expectedName, path, StringComparison.OrdinalIgnoreCase);

        CleanupDirectory(path);
    }

    [Fact]
    public void GetGamePath_ReturnsPathUnderUserAndGameId()
    {
        var userId = "SAM.Tests.UserGame";
        const uint gameId = 440;
        var expectedUser = InvokeSanitizeFileName(userId);

        var path = AppPaths.GetGamePath(userId, gameId);

        var expectedSuffix = Path.Combine(expectedUser, gameId.ToString());
        Assert.True(Directory.Exists(path));
        Assert.StartsWith(AppPaths.UserdataPath, path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(expectedSuffix, path, StringComparison.OrdinalIgnoreCase);

        CleanupDirectory(AppPaths.GetUserPath(userId));
    }

    [Fact]
    public void GetAllUsers_ReturnsExistingUserDirectories()
    {
        var user1 = "SAM.Tests.UserA";
        var user2 = "SAM.Tests.UserB";

        var path1 = AppPaths.GetUserPath(user1);
        var path2 = AppPaths.GetUserPath(user2);

        var users = AppPaths.GetAllUsers().ToList();

        Assert.Contains(InvokeSanitizeFileName(user1), users);
        Assert.Contains(InvokeSanitizeFileName(user2), users);

        CleanupDirectory(path1);
        CleanupDirectory(path2);
    }

    [Fact]
    public void GetUserGames_ReturnsOnlyNumericGameFolders()
    {
        var userId = "SAM.Tests.UserGames";
        var userPath = AppPaths.GetUserPath(userId);

        Directory.CreateDirectory(Path.Combine(userPath, "440"));
        Directory.CreateDirectory(Path.Combine(userPath, "not-a-game"));

        var games = AppPaths.GetUserGames(userId).ToList();

        Assert.Contains("440", games);
        Assert.DoesNotContain("not-a-game", games);

        CleanupDirectory(userPath);
    }

    [Fact]
    public void SanitizeFileName_ReplacesInvalidCharacters()
    {
        var input = "a<b>c|d";
        var result = InvokeSanitizeFileName(input);

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            Assert.DoesNotContain(invalid, result);
        }

        Assert.Contains("a", result);
        Assert.Contains("b", result);
        Assert.Contains("c", result);
        Assert.Contains("d", result);
    }

    [Fact]
    public void SanitizeFileName_EmptyString_ReturnsEmpty()
    {
        var result = InvokeSanitizeFileName(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeFileName_InvalidOnly_ReturnsEmpty()
    {
        var input = new string(Path.GetInvalidFileNameChars());
        var result = InvokeSanitizeFileName(input);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeFileName_Null_Throws()
    {
        var method = typeof(AppPaths).GetMethod("SanitizeFileName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        Assert.Throws<TargetInvocationException>(() => method!.Invoke(null, [null]));
    }

    [Fact]
    public void CleanupOldLogs_RemovesOldKeepsNew()
    {
        var logsPath = AppPaths.LogsPath;
        Directory.CreateDirectory(logsPath);

        var oldLog = Path.Combine(logsPath, "sam_tests_old.log");
        var newLog = Path.Combine(logsPath, "sam_tests_new.log");

        File.WriteAllText(oldLog, "old");
        File.WriteAllText(newLog, "new");

        File.SetCreationTime(oldLog, DateTime.Now.AddDays(-2));
        File.SetCreationTime(newLog, DateTime.Now);

        AppPaths.CleanupOldLogs(TimeSpan.FromHours(1));

        Assert.False(File.Exists(oldLog));
        Assert.True(File.Exists(newLog));

        CleanupFile(newLog);
    }

    private static string InvokeSanitizeFileName(string name)
    {
        var method = typeof(AppPaths).GetMethod("SanitizeFileName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, [name])!;
    }

    private static void CleanupDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for local app data.
        }
    }

    private static void CleanupFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup for local app data.
        }
    }
}
