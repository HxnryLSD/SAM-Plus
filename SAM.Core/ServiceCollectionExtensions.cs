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

using Microsoft.Extensions.DependencyInjection;
using SAM.Core.Services;
using SAM.Core.ViewModels;
using System.Net.Http;

namespace SAM.Core;

/// <summary>
/// Extension methods for registering SAM.Core services in DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
        /// Adds SAM.Core services to the service collection.
        /// </summary>
        public static IServiceCollection AddSamCoreServices(this IServiceCollection services)
        {
            // Register services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IUserDataService, UserDataService>();
            services.AddSingleton<IDrmProtectionService, DrmProtectionService>();
            services.AddSingleton<ISteamService, SteamService>();
            services.AddSingleton<ISteamCallbackService, SteamCallbackService>();
            services.AddSingleton<ILegacyMigrationService, LegacyMigrationService>();
            services.AddSingleton<ILibraryFetchService, LibraryFetchService>();
            services.AddSingleton<IGameCacheService, GameCacheService>();
            services.AddTransient<IAchievementService, AchievementService>();

            services.AddTransient<Func<ISteamService>>(sp => () => sp.GetRequiredService<ISteamService>());
            services.AddTransient<Func<IImageCacheService>>(sp => () => sp.GetRequiredService<IImageCacheService>());
            services.AddTransient<Func<IUserDataService>>(sp => () => sp.GetRequiredService<IUserDataService>());
            services.AddTransient<Func<IGameCacheService>>(sp => () => sp.GetRequiredService<IGameCacheService>());

            services.AddHttpClient<IUpdateService, UpdateService>()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SAM/8.0");
                });
        
            // Register HttpClient for ImageCacheService using typed client pattern
            // Enable HTTP/2 for multiplexed parallel downloads
            services.AddHttpClient<IImageCacheService, ImageCacheService>()
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    // Enable HTTP/2 for multiplexing (parallel requests over single connection)
                    EnableMultipleHttp2Connections = true,
                    // Connection pooling for better performance
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                    MaxConnectionsPerServer = 10,
                    // Keep-alive for connection reuse
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10)
                })
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SAM/8.0");
                    // Request HTTP/2 by default
                    client.DefaultRequestVersion = System.Net.HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                });
        
            // Register ViewModels
            services.AddSingleton<GamePickerViewModel>();
            services.AddTransient<AchievementManagerViewModel>();
        
            return services;
        }
    }
