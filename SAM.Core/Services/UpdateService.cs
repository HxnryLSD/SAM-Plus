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
using System.Text.Json.Serialization;

namespace SAM.Core.Services;

/// <summary>
/// Checks GitHub Releases for newer versions.
/// </summary>
public class UpdateService : IUpdateService
{
    private const string LatestReleaseUrl = "https://github.com/HxnryLSD/SAM-Plus/releases/latest";
    private readonly HttpClient _httpClient;

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        if (!TryParseVersion(currentVersion, out var current))
        {
            return new UpdateCheckResult
            {
                ErrorMessage = $"Invalid current version: {currentVersion}"
            };
        }

        try
        {
            using var response = await _httpClient.GetAsync(LatestReleaseUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    ErrorMessage = $"Update check failed: {response.StatusCode}"
                };
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);
            if (release?.TagName == null)
            {
                return new UpdateCheckResult
                {
                    ErrorMessage = "Update check failed: missing release tag"
                };
            }

            if (!TryParseVersion(release.TagName, out var latest))
            {
                return new UpdateCheckResult
                {
                    ErrorMessage = $"Invalid release tag: {release.TagName}"
                };
            }

            if (latest <= current)
            {
                return new UpdateCheckResult
                {
                    IsUpdateAvailable = false,
                    LatestVersion = latest.ToString(),
                    ReleaseUrl = release.HtmlUrl
                };
            }

            return new UpdateCheckResult
            {
                IsUpdateAvailable = true,
                LatestVersion = latest.ToString(),
                ReleaseUrl = release.HtmlUrl
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                ErrorMessage = ex.Message
            };
        }
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[1..];
        }

        return Version.TryParse(trimmed, out version!);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }
    }
}
