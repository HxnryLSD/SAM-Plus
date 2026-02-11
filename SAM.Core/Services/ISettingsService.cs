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
/// Interface for application settings service.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets the application theme (Light, Dark, System).
    /// </summary>
    string Theme { get; set; }

    /// <summary>
    /// Gets or sets the application language code (e.g., "en", "de").
    /// </summary>
    string Language { get; set; }

    /// <summary>
    /// Gets or sets whether to show only games with achievements.
    /// </summary>
    bool ShowOnlyGamesWithAchievements { get; set; }

    /// <summary>
    /// Gets or sets the default game filter type.
    /// </summary>
    int DefaultGameFilter { get; set; }

    /// <summary>
    /// Gets or sets whether to warn on unsaved changes.
    /// </summary>
    bool WarnOnUnsavedChanges { get; set; }

    /// <summary>
    /// Gets or sets whether to show hidden achievements.
    /// </summary>
    bool ShowHiddenAchievements { get; set; }

    /// <summary>
    /// Gets or sets the game list view type (0=Default, 1=Compact, 2=Detail).
    /// </summary>
    int GameViewType { get; set; }

    /// <summary>
    /// Gets or sets the image cache directory path.
    /// </summary>
    string ImageCachePath { get; }

    /// <summary>
    /// Loads settings from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves settings to storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all settings to default values.
    /// </summary>
    void ResetToDefaults();
}
