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

using System.Diagnostics;
using SAM.Core.Services;

namespace SAM.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of IImageCacheService for testing.
/// </summary>
public class MockImageCacheService : IImageCacheService
{
    private readonly Dictionary<string, byte[]> _cache = new();
    
    public long MaxCacheSizeBytes { get; set; } = 100 * 1024 * 1024;

    public void SetImage(string url, byte[] data)
    {
        _cache[url] = data;
    }

    public Task<byte[]?> GetImageAsync(string url, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(url, out var data);
        return Task.FromResult(data);
    }

    public async Task<BatchDownloadResult> GetImagesAsync(
        IEnumerable<string> urls, 
        int maxParallelism = 10, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var urlList = urls.ToList();
        int successCount = 0;
        int cacheHitCount = 0;

        foreach (var url in urlList)
        {
            if (_cache.ContainsKey(url))
            {
                cacheHitCount++;
            }
            else
            {
                // Simulate download with empty data
                _cache[url] = [];
            }
            successCount++;
        }

        stopwatch.Stop();
        return new BatchDownloadResult
        {
            TotalRequested = urlList.Count,
            SuccessCount = successCount,
            FailedCount = 0,
            CacheHitCount = cacheHitCount,
            Duration = stopwatch.Elapsed
        };
    }

    public string OptimizeSteamUrl(string url) => url;

    public void ClearCache()
    {
        _cache.Clear();
    }

    public bool IsCached(string url)
    {
        return _cache.ContainsKey(url);
    }

    public ImageCacheStatistics GetStatistics()
    {
        var totalSize = _cache.Values.Sum(v => (long)v.Length);
        return new ImageCacheStatistics
        {
            TotalSizeBytes = totalSize,
            FileCount = _cache.Count,
            MemoryCacheCount = _cache.Count,
            MemoryCacheSizeBytes = totalSize,
            MaxSizeBytes = MaxCacheSizeBytes,
            ConditionalHits = 0,
            ConditionalMisses = 0
        };
    }

    public Task EvictOldEntriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
