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
public interface ILegacyMigrationService
{
    /// <summary>
    /// Checks if legacy data exists that can be migrated.
    /// </summary>
    bool HasLegacyData { get; }

    /// <summary>
    /// Gets the path to the legacy image cache.
    /// </summary>
    string? LegacyImageCachePath { get; }

    /// <summary>
    /// Migrates settings from the legacy application.
    /// </summary>
    Task<bool> MigrateSettingsAsync();

    /// <summary>
    /// Migrates the image cache from the legacy application.
    /// </summary>
    Task<bool> MigrateImageCacheAsync(IProgress<int>? progress = null);

    /// <summary>
    /// Marks migration as complete (won't prompt again).
    /// </summary>
    void MarkMigrationComplete();

    /// <summary>
    /// Checks if migration has already been completed.
    /// </summary>
    bool IsMigrationComplete { get; }
}
