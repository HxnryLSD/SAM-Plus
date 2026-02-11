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
/// Image cache statistics.
/// </summary>
public record ImageCacheStatistics
{
    public long TotalSizeBytes { get; init; }
    public int FileCount { get; init; }
    public int MemoryCacheCount { get; init; }
    public long MemoryCacheSizeBytes { get; init; }
    public long MaxSizeBytes { get; init; }
    public double UsagePercent => MaxSizeBytes > 0 ? (double)TotalSizeBytes / MaxSizeBytes * 100 : 0;
    public int ConditionalHits { get; init; }
    public int ConditionalMisses { get; init; }
}

/// <summary>
/// Result of a batch download operation.
/// </summary>
public record BatchDownloadResult
{
    public int TotalRequested { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public int CacheHitCount { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Service for caching and downloading images with LRU eviction.
/// Supports HTTP/2 multiplexing, conditional requests, and batch downloads.
/// </summary>
public interface IImageCacheService
{
    /// <summary>
    /// Maximum cache size in bytes (default: 100MB).
    /// </summary>
    long MaxCacheSizeBytes { get; set; }

    /// <summary>
    /// Gets an image from cache or downloads it.
    /// Uses conditional requests (ETag/If-Modified-Since) when available.
    /// </summary>
    /// <param name="url">The URL of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The image data as byte array, or null if not found.</returns>
    Task<byte[]?> GetImageAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads multiple images in parallel using HTTP/2 multiplexing.
    /// </summary>
    /// <param name="urls">The URLs to download.</param>
    /// <param name="maxParallelism">Maximum concurrent downloads (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result summary of the batch operation.</returns>
    Task<BatchDownloadResult> GetImagesAsync(IEnumerable<string> urls, int maxParallelism = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the image cache.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets whether an image is cached.
    /// </summary>
    bool IsCached(string url);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    ImageCacheStatistics GetStatistics();

    /// <summary>
    /// Runs LRU eviction to bring cache under the size limit.
    /// Called automatically but can be invoked manually.
    /// </summary>
    Task EvictOldEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes a Steam CDN URL for best performance.
    /// </summary>
    /// <param name="url">The original Steam image URL.</param>
    /// <returns>The optimized URL.</returns>
    string OptimizeSteamUrl(string url);
}
