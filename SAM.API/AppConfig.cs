/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
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

using System;
using System.IO;
using System.Text.Json;

namespace SAM.API
{
    /// <summary>
    /// Application configuration loaded from appsettings.json.
    /// Provides centralized access to URLs and other configurable values.
    /// </summary>
    public static class AppConfig
    {
        private static readonly Lazy<ConfigData> _config = new(LoadConfig);

        /// <summary>
        /// URL for the game list XML file.
        /// </summary>
        public static string GamesListUrl => _config.Value.Urls.GamesListUrl;

        /// <summary>
        /// Base URL for Steam CDN images.
        /// </summary>
        public static string SteamCdnBaseUrl => _config.Value.Urls.SteamCdnBaseUrl;

        /// <summary>
        /// Base URL for Steam Cloudflare CDN images.
        /// </summary>
        public static string SteamCloudflareBaseUrl => _config.Value.Urls.SteamCloudflareBaseUrl;

        private static ConfigData LoadConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<ConfigData>(json);
                    if (config != null)
                    {
                        Logger.Info($"Configuration loaded from {configPath}");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to load appsettings.json, using defaults");
                }
            }
            
            // Return defaults if file doesn't exist or fails to load
            Logger.Warn("Using default configuration values");
            return new ConfigData
            {
                Urls = new UrlConfig
                {
                    GamesListUrl = "https://gib.me/sam/games.xml",
                    SteamCdnBaseUrl = "https://cdn.steamstatic.com/steamcommunity/public/images/apps",
                    SteamCloudflareBaseUrl = "https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps"
                }
            };
        }

        private class ConfigData
        {
            public UrlConfig Urls { get; set; } = new();
        }

        private class UrlConfig
        {
            public string GamesListUrl { get; set; } = "";
            public string SteamCdnBaseUrl { get; set; } = "";
            public string SteamCloudflareBaseUrl { get; set; } = "";
        }
    }
}
