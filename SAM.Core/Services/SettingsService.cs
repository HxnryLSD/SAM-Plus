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

using System.Text.Json;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Settings service implementation using JSON file storage.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private SettingsData _settings = new();

    public SettingsService()
    {
        _settingsFilePath = AppPaths.SettingsFilePath;
    }

    public string Theme
    {
        get => _settings.Theme;
        set => _settings.Theme = value;
    }

    public string Language
    {
        get => _settings.Language;
        set => _settings.Language = value;
    }

    public bool ShowOnlyGamesWithAchievements
    {
        get => _settings.ShowOnlyGamesWithAchievements;
        set => _settings.ShowOnlyGamesWithAchievements = value;
    }

    public int DefaultGameFilter
    {
        get => _settings.DefaultGameFilter;
        set => _settings.DefaultGameFilter = value;
    }

    public bool WarnOnUnsavedChanges
    {
        get => _settings.WarnOnUnsavedChanges;
        set => _settings.WarnOnUnsavedChanges = value;
    }

    public bool ShowHiddenAchievements
    {
        get => _settings.ShowHiddenAchievements;
        set => _settings.ShowHiddenAchievements = value;
    }

    public int GameViewType
    {
        get => _settings.GameViewType;
        set => _settings.GameViewType = value;
    }

    public string ImageCachePath => _settings.ImageCachePath;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
                var loaded = JsonSerializer.Deserialize<SettingsData>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Don't swallow cancellation
        }
        catch (Exception ex)
        {
            // If loading fails, use default settings
            Log.Warn($"Failed to load settings: {ex.Message}");
            _settings = new SettingsData();
        }

        // Ensure cache directory exists
        Directory.CreateDirectory(_settings.ImageCachePath);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw; // Don't swallow cancellation
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save settings: {ex.Message}");
        }
    }

    public void ResetToDefaults()
    {
        _settings = new SettingsData();
    }

    private class SettingsData
    {
        public string Theme { get; set; } = "System";
        public string Language { get; set; } = "";
        public bool ShowOnlyGamesWithAchievements { get; set; } = false;
        public int DefaultGameFilter { get; set; } = 0;
        public bool WarnOnUnsavedChanges { get; set; } = true;
        public bool ShowHiddenAchievements { get; set; } = true;
        public int GameViewType { get; set; } = 0; // 0=Default, 1=Compact, 2=Detail
        public string ImageCachePath { get; set; } = "";
    }
}
