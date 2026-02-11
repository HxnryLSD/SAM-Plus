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
using Microsoft.Data.Sqlite;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Settings service implementation using JSON file storage.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly string _settingsDatabasePath;
    private SettingsData _settings = new();

    private const string SharedSettingsTable = "Settings";
    private const string SharedSettingsKey = "SettingsData";

    public SettingsService()
    {
        _settingsFilePath = AppPaths.SettingsFilePath;
        _settingsDatabasePath = AppPaths.SettingsDatabasePath;
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

    public bool UseSharedSettings
    {
        get => _settings.UseSharedSettings;
        set => _settings.UseSharedSettings = value;
    }

    public bool AutoUpdateEnabled
    {
        get => _settings.AutoUpdateEnabled;
        set => _settings.AutoUpdateEnabled = value;
    }

    public bool UseSystemAccentColor
    {
        get => _settings.UseSystemAccentColor;
        set => _settings.UseSystemAccentColor = value;
    }

    public string AccentColor
    {
        get => _settings.AccentColor;
        set => _settings.AccentColor = value;
    }

    public string ImageCachePath => _settings.ImageCachePath;

    public long ImageCacheMaxSizeBytes
    {
        get => _settings.ImageCacheMaxSizeBytes;
        set => _settings.ImageCacheMaxSizeBytes = value;
    }

    public bool EnableOfflineMode
    {
        get => _settings.EnableOfflineMode;
        set => _settings.EnableOfflineMode = value;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _settings = await LoadLocalAsync(cancellationToken);

            if (_settings.UseSharedSettings)
            {
                var shared = await LoadSharedAsync(cancellationToken);
                if (shared != null)
                {
                    shared.UseSharedSettings = true;
                    _settings = shared;
                }
                else
                {
                    Log.Warn("Shared settings enabled but no shared settings found. Using local settings.");
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

        if (_settings.ImageCacheMaxSizeBytes <= 0)
        {
            _settings.ImageCacheMaxSizeBytes = 100 * 1024 * 1024;
        }

        if (string.IsNullOrWhiteSpace(_settings.ImageCachePath))
        {
            _settings.ImageCachePath = AppPaths.CachePath;
        }

        // Ensure cache directory exists
        Directory.CreateDirectory(_settings.ImageCachePath);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_settings.UseSharedSettings)
            {
                await SaveSharedAsync(_settings, cancellationToken);
            }

            await SaveLocalAsync(_settings, cancellationToken);
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

    private async Task<SettingsData> LoadLocalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new SettingsData();
        }

        var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
        var loaded = JsonSerializer.Deserialize<SettingsData>(json);
        return loaded ?? new SettingsData();
    }

    private async Task SaveLocalAsync(SettingsData settings, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);
    }

    private async Task<SettingsData?> LoadSharedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_settingsDatabasePath}");
            await connection.OpenAsync(cancellationToken);

            await using (var create = connection.CreateCommand())
            {
                create.CommandText = $"CREATE TABLE IF NOT EXISTS {SharedSettingsTable} (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);";
                await create.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT Value FROM {SharedSettingsTable} WHERE Key = @Key;";
            command.Parameters.AddWithValue("@Key", SharedSettingsKey);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is not string json || string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var loaded = JsonSerializer.Deserialize<SettingsData>(json);
            return loaded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load shared settings: {ex.Message}");
            return null;
        }
    }

    private async Task SaveSharedAsync(SettingsData settings, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_settingsDatabasePath}");
            await connection.OpenAsync(cancellationToken);

            await using (var create = connection.CreateCommand())
            {
                create.CommandText = $"CREATE TABLE IF NOT EXISTS {SharedSettingsTable} (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);";
                await create.ExecuteNonQueryAsync(cancellationToken);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await using var command = connection.CreateCommand();
            command.CommandText = $"INSERT INTO {SharedSettingsTable} (Key, Value) VALUES (@Key, @Value) ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            command.Parameters.AddWithValue("@Key", SharedSettingsKey);
            command.Parameters.AddWithValue("@Value", json);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save shared settings: {ex.Message}");
        }
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
        public bool UseSharedSettings { get; set; } = false;
        public bool AutoUpdateEnabled { get; set; } = true;
        public bool UseSystemAccentColor { get; set; } = true;
        public string AccentColor { get; set; } = "#FF0078D4";
        public string ImageCachePath { get; set; } = "";
        public long ImageCacheMaxSizeBytes { get; set; } = 100 * 1024 * 1024;
        public bool EnableOfflineMode { get; set; } = true;
    }
}
