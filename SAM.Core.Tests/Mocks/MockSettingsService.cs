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

namespace SAM.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of ISettingsService for testing.
/// </summary>
public class MockSettingsService : ISettingsService
{
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "";
    public bool ShowOnlyGamesWithAchievements { get; set; }
    public int DefaultGameFilter { get; set; }
    public bool WarnOnUnsavedChanges { get; set; } = true;
    public bool ShowHiddenAchievements { get; set; } = true;
    public int GameViewType { get; set; }
    public bool UseSharedSettings { get; set; }
    public bool AutoUpdateEnabled { get; set; } = true;
    public bool UseSystemAccentColor { get; set; } = true;
    public string AccentColor { get; set; } = "#FF0078D4";
    public string ImageCachePath { get; set; } = Path.Combine(Path.GetTempPath(), "SAM.Tests", "ImageCache");
    public long ImageCacheMaxSizeBytes { get; set; } = 100 * 1024 * 1024;
    public bool EnableOfflineMode { get; set; } = true;
    public bool HasWindowPlacement { get; set; }
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }

    public bool LoadAsyncCalled { get; private set; }
    public bool SaveAsyncCalled { get; private set; }
    public bool ResetCalled { get; private set; }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        LoadAsyncCalled = true;
        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        SaveAsyncCalled = true;
        return Task.CompletedTask;
    }

    public void ResetToDefaults()
    {
        ResetCalled = true;
        Theme = "System";
        Language = "";
        ShowOnlyGamesWithAchievements = false;
        DefaultGameFilter = 0;
        WarnOnUnsavedChanges = true;
        ShowHiddenAchievements = true;
        GameViewType = 0;
        UseSharedSettings = false;
        AutoUpdateEnabled = true;
        UseSystemAccentColor = true;
        AccentColor = "#FF0078D4";
        ImageCacheMaxSizeBytes = 100 * 1024 * 1024;
        EnableOfflineMode = true;
        HasWindowPlacement = false;
        WindowX = 0;
        WindowY = 0;
        WindowWidth = 0;
        WindowHeight = 0;
    }
}
