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

using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.XPath;
using SAM.API;
using SAM.API.Wrappers;
using SAM.Core.Models;

namespace SAM.Core.Services;

/// <summary>
/// Steam service implementation wrapping SAM.API.Client.
/// </summary>
public class SteamService : ISteamService
{
    private static readonly HttpClient _httpClient;
    private const string GamesListUrl = "https://gib.me/sam/games.xml";
    
    private readonly IDrmProtectionService _drmService;
    private readonly IUserDataService _userDataService;
    private Client? _client;
    private bool _disposed;

    static SteamService()
    {
        // Configure HttpClient with proper TLS settings
        var handler = new HttpClientHandler
        {
            // Allow all TLS versions
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                           System.Security.Authentication.SslProtocols.Tls13,
            // Only allow certificate exceptions for gib.me.
            ServerCertificateCustomValidationCallback = ValidateServerCertificate
        };
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SAM.WinUI/1.0");
    }

    public SteamService(IDrmProtectionService drmService, IUserDataService userDataService)
    {
        _drmService = drmService;
        _userDataService = userDataService;
    }

    public bool IsInitialized => _client is not null;

    public long CurrentAppId { get; private set; }

    public ulong SteamId => _client?.SteamUser?.GetSteamId() ?? 0;

    public Client? Client => _client;

    public SteamUserStats013? SteamUserStats => _client?.SteamUserStats;

    public SteamApps008? SteamApps008 => _client?.SteamApps008;

    public void Initialize(long appId = 0)
    {
        if (_client is not null)
        {
            return;
        }

        _client = new Client();
        try
        {
            _client.Initialize(appId);
            CurrentAppId = appId;
        }
        catch
        {
            _client.Dispose();
            _client = null;
            throw;
        }
    }

    public void InitializeForGame(long gameId)
    {
        Log.Debug($"InitializeForGame called with gameId={gameId}");
        // Dispose old client if exists
        if (_client is not null)
        {
            Log.Debug("Disposing existing client");
            _client.Dispose();
            _client = null;
        }

        // Create new client with game-specific AppID
        Log.Debug("Creating new Client");
        _client = new Client();
        try
        {
            Log.Debug($"Calling _client.Initialize({gameId})");
            _client.Initialize(gameId);
            CurrentAppId = gameId;
            Log.Debug($"Client initialized successfully. CurrentAppId={CurrentAppId}, SteamId={SteamId}");
            
            // Set current user for UserDataService
            if (SteamId > 0)
            {
                _userDataService.SetCurrentUser(SteamId.ToString());
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Client initialization FAILED");
            _client.Dispose();
            _client = null;
            throw;
        }
    }

    public async Task<IEnumerable<GameModel>> GetOwnedGamesAsync(CancellationToken cancellationToken = default)
    {
        if (_client?.SteamApps008 is null || _client?.SteamApps001 is null)
        {
            throw new InvalidOperationException("Steam client not initialized. SteamApps008 or SteamApps001 is null.");
        }

        var games = new List<GameModel>();
        List<KeyValuePair<uint, string>> gameIds = [];

        // Step 1: Download games list from remote server
        using var response = await _httpClient.GetAsync(GamesListUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        
        using var stream = new MemoryStream(bytes, false);
        var document = new XPathDocument(stream);
        var navigator = document.CreateNavigator();
        var nodes = navigator.Select("/games/game");

        while (nodes.MoveNext())
        {
            var id = (uint)nodes.Current!.ValueAsLong;
            var typeStr = nodes.Current.GetAttribute("type", "");
            if (string.IsNullOrEmpty(typeStr))
            {
                typeStr = "normal";
            }
            gameIds.Add(new(id, typeStr));
        }

        // Step 2: Check ownership for each game
        foreach (var kv in gameIds)
        {
            var id = kv.Key;
            var typeStr = kv.Value;

            // Check if user owns this game
            if (!_client.SteamApps008.IsSubscribedApp(id))
            {
                continue;
            }

            // Get game name from Steam
            var name = _client.SteamApps001.GetAppData(id, "name");
            if (string.IsNullOrEmpty(name))
            {
                name = $"Unknown Game ({id})";
            }

            // Map type string to GameType enum
            var gameType = typeStr.ToLowerInvariant() switch
            {
                "normal" => GameType.Game,
                "demo" => GameType.Demo,
                "mod" => GameType.Mod,
                "junk" => GameType.Unknown,
                "dlc" => GameType.Dlc,
                _ => GameType.Game
            };

            // Get image URL
            var imageUrl = GetGameImageUrl(id);

            // Note: DRM protection is checked lazily when viewing achievements.
            // Checking 200+ schema files during game list loading would be too slow.

            games.Add(new GameModel
            {
                Id = id,
                Name = name,
                Type = gameType,
                ImageUrl = imageUrl
            });
        }

        return games.OrderBy(g => g.Name);
    }

    private static bool ValidateServerCertificate(
        HttpRequestMessage? message,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        var host = message?.RequestUri?.Host;
        if (!string.IsNullOrEmpty(host) && string.Equals(host, "gib.me", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private string? GetGameImageUrl(uint id)
    {
        if (_client?.SteamApps001 is null)
        {
            return null;
        }

        var currentLanguage = _client.SteamApps008?.GetCurrentGameLanguage() ?? "english";

        // Try small capsule image first (current language)
        var candidate = _client.SteamApps001.GetAppData(id, $"small_capsule/{currentLanguage}");
        if (!string.IsNullOrEmpty(candidate))
        {
            return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{candidate}";
        }

        // Try English small capsule
        if (currentLanguage != "english")
        {
            candidate = _client.SteamApps001.GetAppData(id, "small_capsule/english");
            if (!string.IsNullOrEmpty(candidate))
            {
                return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{candidate}";
            }
        }

        // Try logo
        candidate = _client.SteamApps001.GetAppData(id, "logo");
        if (!string.IsNullOrEmpty(candidate))
        {
            return $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{id}/{candidate}.jpg";
        }

        // Fallback to Steam CDN header image
        return $"https://cdn.cloudflare.steamstatic.com/steam/apps/{id}/header.jpg";
    }

    public string? GetGameName(uint appId)
    {
        return _client?.SteamApps001?.GetAppData(appId, "name");
    }

    public string? GetCurrentGameLanguage()
    {
        return _client?.SteamApps008?.GetCurrentGameLanguage();
    }

    public bool OwnsGame(uint appId)
    {
        return _client?.SteamApps008?.IsSubscribedApp(appId) ?? false;
    }

    public string? GetGameLogoUrl(uint appId)
    {
        var logo = _client?.SteamApps001?.GetAppData(appId, "logo");
        if (string.IsNullOrEmpty(logo))
        {
            return null;
        }

        return $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{appId}/{logo}.jpg";
    }

    public void RunCallbacks()
    {
        _client?.RunCallbacks(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client?.Dispose();
                _client = null;
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
