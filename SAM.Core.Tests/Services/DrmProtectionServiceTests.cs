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

using System.Globalization;
using System.Reflection;
using System.Text;
using SAM.API.Types;
using SAM.Core.Services;

namespace SAM.Core.Tests.Services;

public class DrmProtectionServiceTests
{
    [Fact]
    public void CheckGameProtection_MissingSchema_ReturnsNull()
    {
        using var tempRoot = new TempDirectory();
        var service = CreateService(tempRoot.Path);

        var result = service.CheckGameProtection(123u);

        Assert.Null(result);
    }

    [Fact]
    public void CheckGameProtection_ProtectedAchievements_ReturnsProtectionInfo()
    {
        using var tempRoot = new TempDirectory();
        var service = CreateService(tempRoot.Path);
        const uint gameId = 440;

        WriteSchemaFile(tempRoot.Path, gameId, [0, 1]);

        var result = service.CheckGameProtection(gameId);

        Assert.NotNull(result);
        Assert.True(result!.HasProtection);
        Assert.Equal(2, result.TotalAchievements);
        Assert.Equal(1, result.ProtectedCount);
        Assert.Contains("1 von 2 Erfolgen", result.Description);
    }

    [Fact]
    public void CheckGameProtection_NoProtectedAchievements_ReturnsNoProtection()
    {
        using var tempRoot = new TempDirectory();
        var service = CreateService(tempRoot.Path);
        const uint gameId = 441;

        WriteSchemaFile(tempRoot.Path, gameId, [0, 0]);

        var result = service.CheckGameProtection(gameId);

        Assert.NotNull(result);
        Assert.False(result!.HasProtection);
        Assert.Equal(2, result.TotalAchievements);
        Assert.Equal(0, result.ProtectedCount);
        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public void CheckGameProtection_PermissionBit2_IsProtected()
    {
        using var tempRoot = new TempDirectory();
        var service = CreateService(tempRoot.Path);
        const uint gameId = 442;

        WriteSchemaFile(tempRoot.Path, gameId, [2]);

        var result = service.CheckGameProtection(gameId);

        Assert.NotNull(result);
        Assert.True(result!.HasProtection);
        Assert.Equal(1, result.TotalAchievements);
        Assert.Equal(1, result.ProtectedCount);
    }

    [Fact]
    public void CheckGameProtection_InvalidSchema_ReturnsReadableErrorInfo()
    {
        using var tempRoot = new TempDirectory();
        var service = CreateService(tempRoot.Path);
        const uint gameId = 443;

        var schemaPath = GetSchemaFilePath(tempRoot.Path, gameId);
        Directory.CreateDirectory(Path.GetDirectoryName(schemaPath)!);
        File.WriteAllBytes(schemaPath, [0xFF, 0x00, 0xAA]);

        var result = service.CheckGameProtection(gameId);

        Assert.NotNull(result);
        Assert.False(result!.HasProtection);
        Assert.Equal(0, result.TotalAchievements);
        Assert.Equal(0, result.ProtectedCount);
        Assert.Equal("Schema konnte nicht gelesen werden", result.Description);
    }

    [Fact]
    public void CheckGameProtection_UsesCacheOnSecondCall()
    {
        using var tempRoot = new TempDirectory();
        var service = CreateService(tempRoot.Path);
        const uint gameId = 444;

        WriteSchemaFile(tempRoot.Path, gameId, [1]);
        var first = service.CheckGameProtection(gameId);

        WriteSchemaFile(tempRoot.Path, gameId, [0]);
        var second = service.CheckGameProtection(gameId);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ProtectedCount, second!.ProtectedCount);
    }

    private static DrmProtectionService CreateService(string steamRoot)
    {
        var service = new DrmProtectionService();
        var field = typeof(DrmProtectionService)
            .GetField("_steamInstallPath", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(service, steamRoot);
        return service;
    }

    private static string GetSchemaFilePath(string steamRoot, uint gameId)
    {
        var fileName = string.Format(CultureInfo.InvariantCulture, "UserGameStatsSchema_{0}.bin", gameId);
        return Path.Combine(steamRoot, "appcache", "stats", fileName);
    }

    private static void WriteSchemaFile(string steamRoot, uint gameId, int[] permissions)
    {
        var path = GetSchemaFilePath(steamRoot, gameId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        WriteNode(writer, KvNone, gameId.ToString(CultureInfo.InvariantCulture),
        [
            new KvNode("stats", KvNone, Children:
            [
                new KvNode("0", KvNone, Children:
                [
                    new KvNode("type_int", KvInt32, Value: (int)UserStatType.Achievements),
                    new KvNode("bits", KvNone, Children: BuildBits(permissions))
                ]),
                new KvNode("1", KvNone, Children:
                [
                    new KvNode("type_int", KvInt32, Value: (int)UserStatType.Integer),
                    new KvNode("bits", KvNone, Children: BuildBits([0]))
                ])
            ])
        ]);

        writer.Write(KvEnd);
    }

    private static KvNode[] BuildBits(int[] permissions)
    {
        var bits = new List<KvNode>();
        for (int i = 0; i < permissions.Length; i++)
        {
            bits.Add(new KvNode(i.ToString(CultureInfo.InvariantCulture), KvNone, Children:
            [
                new KvNode("permission", KvInt32, Value: permissions[i])
            ]));
        }
        return bits.ToArray();
    }

    private static void WriteNode(BinaryWriter writer, byte type, string name, KvNode[]? children = null, object? value = null)
    {
        writer.Write(type);
        WriteString(writer, name);

        if (type == KvNone)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    WriteNode(writer, child.Type, child.Name, child.Children, child.Value);
                }
            }
            writer.Write(KvEnd);
            return;
        }

        if (type == KvString)
        {
            WriteString(writer, value?.ToString() ?? string.Empty);
            return;
        }

        if (type == KvInt32)
        {
            writer.Write((int)(value ?? 0));
            return;
        }

        throw new InvalidOperationException("Unsupported KV type in tests");
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes);
        writer.Write((byte)0);
    }

    private sealed record KvNode(string Name, byte Type, object? Value = null, KvNode[]? Children = null);

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SAM.Tests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private const byte KvNone = 0;
    private const byte KvString = 1;
    private const byte KvInt32 = 2;
    private const byte KvEnd = 8;
}
