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

namespace SAM.Core.Services;

/// <summary>
/// Service for migrating data from the legacy WinForms application.
/// </summary>
public class LegacyMigrationService : ILegacyMigrationService
{
    private readonly ISettingsService _settingsService;
    private readonly string _legacyCachePath;
    private readonly string _migrationFlagPath;
    private readonly string _newCachePath;

    public LegacyMigrationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Legacy path: %TEMP%\SAM (used by old WinForms app)
        _legacyCachePath = Path.Combine(Path.GetTempPath(), "SAM");
        
        // New path: %LOCALAPPDATA%\SAM
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SAM");
        
        _migrationFlagPath = Path.Combine(appDataPath, ".migration_complete");
        _newCachePath = Path.Combine(appDataPath, "ImageCache");
    }

    public bool HasLegacyData => Directory.Exists(_legacyCachePath) && 
                                  Directory.GetFiles(_legacyCachePath, "*.jpg", SearchOption.AllDirectories).Length > 0;

    public string? LegacyImageCachePath => HasLegacyData ? _legacyCachePath : null;

    public bool IsMigrationComplete => File.Exists(_migrationFlagPath);

    public async Task<bool> MigrateSettingsAsync()
    {
        try
        {
            // The legacy app didn't have persistent settings in the same way
            // This is a placeholder for future settings migration if needed
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug($"MigrateSettingsAsync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MigrateImageCacheAsync(IProgress<int>? progress = null)
    {
        try
        {
            if (!HasLegacyData)
            {
                return true;
            }

            // Ensure new cache directory exists
            Directory.CreateDirectory(_newCachePath);

            var files = Directory.GetFiles(_legacyCachePath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var totalFiles = files.Count;
            var processedFiles = 0;

            foreach (var sourceFile in files)
            {
                try
                {
                    // Preserve subdirectory structure
                    var relativePath = Path.GetRelativePath(_legacyCachePath, sourceFile);
                    var destFile = Path.Combine(_newCachePath, relativePath);
                    var destDir = Path.GetDirectoryName(destFile);

                    if (destDir != null && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Copy file if it doesn't exist in new location
                    if (!File.Exists(destFile))
                    {
                        await Task.Run(() => File.Copy(sourceFile, destFile, overwrite: false));
                    }
                }
                catch (Exception ex)
                {
                    // Skip files that fail to copy
                    Log.Debug($"Failed to copy {sourceFile}: {ex.Message}");
                }

                processedFiles++;
                progress?.Report((int)((double)processedFiles / totalFiles * 100));
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"MigrateImageCacheAsync failed: {ex.Message}");
            return false;
        }
    }

    public void MarkMigrationComplete()
    {
        try
        {
            var dir = Path.GetDirectoryName(_migrationFlagPath);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(_migrationFlagPath, DateTime.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to mark migration complete: {ex.Message}");
        }
    }
}
