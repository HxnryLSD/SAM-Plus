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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SAM.Core.Utilities;

namespace SAM.Core.Services;

/// <summary>
/// Metadata for cached images (ETag, Last-Modified).
/// </summary>
internal record CacheMetadata
{
    public string? ETag { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public DateTime CachedAt { get; init; }
}

/// <summary>
/// Image caching service with LRU eviction, HTTP/2 multiplexing, 
/// conditional requests (ETag/If-Modified-Since), and batch downloads.
/// Default max size: 100MB.
/// </summary>
public class ImageCacheService : IImageCacheService
{
    private sealed class MemoryCacheEntry
    {
        public WeakReference<byte[]> DataRef { get; }
        public DateTime LastAccess { get; set; }
        public long SizeBytes { get; }

        public MemoryCacheEntry(byte[] data)
        {
            DataRef = new WeakReference<byte[]>(data);
            LastAccess = DateTime.UtcNow;
            SizeBytes = data.Length;
        }
    }

    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, MemoryCacheEntry> _memoryCache = new();
    private readonly ConcurrentDictionary<string, CacheMetadata> _metadataCache = new();
    private readonly string _coverCacheDirectory;
    private readonly string _widecoverCacheDirectory;
    private readonly string _metadataDirectory;
    private readonly SemaphoreSlim _evictionLock = new(1, 1);
    private long _currentCacheSize;
    private bool _evictionInProgress;
    private int _conditionalHits;
    private int _conditionalMisses;

    /// <summary>
    /// Default max cache size: 100MB
    /// </summary>
    private const long DefaultMaxCacheSizeBytes = 100 * 1024 * 1024;

    /// <summary>
    /// Memory cache max size: 50MB (subset of total cache)
    /// </summary>
    private const long MaxMemoryCacheSizeBytes = 50 * 1024 * 1024;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public long MaxCacheSizeBytes { get; set; } = DefaultMaxCacheSizeBytes;

    public ImageCacheService(HttpClient httpClient, ISettingsService? settingsService = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;

        if (settingsService is not null && settingsService.ImageCacheMaxSizeBytes > 0)
        {
            MaxCacheSizeBytes = settingsService.ImageCacheMaxSizeBytes;
        }
        
        // Migrate legacy ImageCache folder to new structure
        AppPaths.MigrateLegacyImageCache();
        
        _coverCacheDirectory = AppPaths.CoverCachePath;
        _widecoverCacheDirectory = AppPaths.WidecoverCachePath;
        _metadataDirectory = Path.Combine(AppPaths.CachePath, "Metadata");
        Directory.CreateDirectory(_metadataDirectory);

        // Calculate current cache size
        _currentCacheSize = CalculateDiskCacheSize();
        
        // Load metadata cache
        LoadMetadataCache();
    }

    public async Task<byte[]?> GetImageAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Optimize URL for Steam CDN
        var optimizedUrl = OptimizeSteamUrl(url);

        // Check memory cache first
        if (_memoryCache.TryGetValue(optimizedUrl, out var cached))
        {
            if (cached.DataRef.TryGetTarget(out var data))
            {
                cached.LastAccess = DateTime.UtcNow;
                return data;
            }

            _memoryCache.TryRemove(optimizedUrl, out _);
        }

        // Determine cache directory based on URL type
        var cacheDirectory = GetCacheDirectoryForUrl(optimizedUrl);
        var cacheKey = GetCacheKey(optimizedUrl);
        var cachePath = Path.Combine(cacheDirectory, cacheKey);
        
        // Check disk cache and try conditional request
        if (File.Exists(cachePath))
        {
            // Try conditional request if we have metadata
            if (_metadataCache.TryGetValue(optimizedUrl, out var metadata))
            {
                var conditionalResult = await TryConditionalRequestAsync(optimizedUrl, metadata, cachePath, cancellationToken);
                if (conditionalResult != null)
                {
                    return conditionalResult;
                }
            }
            else
            {
                // No metadata, just return cached file
                var diskData = await File.ReadAllBytesAsync(cachePath, cancellationToken);
                UpdateFileAccessTime(cachePath);
                AddToMemoryCache(optimizedUrl, diskData);
                return diskData;
            }
        }

        // Download from URL
        return await DownloadImageAsync(optimizedUrl, cachePath, cancellationToken);
    }

    private async Task<byte[]?> TryConditionalRequestAsync(
        string url, 
        CacheMetadata metadata, 
        string cachePath, 
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Add conditional headers
            if (!string.IsNullOrEmpty(metadata.ETag))
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{metadata.ETag}\""));
            }
            if (metadata.LastModified.HasValue)
            {
                request.Headers.IfModifiedSince = metadata.LastModified.Value;
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                // Resource hasn't changed, use cached version
                Interlocked.Increment(ref _conditionalHits);
                var diskData = await File.ReadAllBytesAsync(cachePath, cancellationToken);
                UpdateFileAccessTime(cachePath);
                AddToMemoryCache(url, diskData);
                return diskData;
            }

            // Resource changed, download new version
            Interlocked.Increment(ref _conditionalMisses);
            return await ProcessResponseAsync(url, response, cachePath, cancellationToken);
        }
        catch
        {
            // On error, fall back to cached version
            if (File.Exists(cachePath))
            {
                var diskData = await File.ReadAllBytesAsync(cachePath, cancellationToken);
                AddToMemoryCache(url, diskData);
                return diskData;
            }
            return null;
        }
    }

    private async Task<byte[]?> DownloadImageAsync(string url, string cachePath, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return await ProcessResponseAsync(url, response, cachePath, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private async Task<byte[]> ProcessResponseAsync(
        string url, 
        HttpResponseMessage response, 
        string cachePath, 
        CancellationToken cancellationToken)
    {
        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        
        // Store in memory cache
        AddToMemoryCache(url, data);
        
        // Store in disk cache
        await File.WriteAllBytesAsync(cachePath, data, cancellationToken);
        
        // Save metadata (ETag, Last-Modified)
        await SaveMetadataAsync(url, response, cancellationToken);
        
        // Update cache size and check for eviction
        Interlocked.Add(ref _currentCacheSize, data.Length);
        
        // Trigger eviction in background if needed
        if (_currentCacheSize > MaxCacheSizeBytes && !_evictionInProgress)
        {
            _ = Task.Run(() => EvictOldEntriesAsync(CancellationToken.None));
        }
        
        return data;
    }

    public async Task<BatchDownloadResult> GetImagesAsync(
        IEnumerable<string> urls, 
        int maxParallelism = 10, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var urlList = urls.ToList();
        
        int successCount = 0;
        int failedCount = 0;
        int cacheHitCount = 0;

        // Use SemaphoreSlim for controlled parallelism
        using var semaphore = new SemaphoreSlim(maxParallelism);
        
        var tasks = urlList.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // Check if already cached
                if (IsCached(url))
                {
                    Interlocked.Increment(ref cacheHitCount);
                    // Still touch the cache entry
                    await GetImageAsync(url, cancellationToken);
                    return true;
                }

                var result = await GetImageAsync(url, cancellationToken);
                if (result != null)
                {
                    Interlocked.Increment(ref successCount);
                    return true;
                }
                else
                {
                    Interlocked.Increment(ref failedCount);
                    return false;
                }
            }
            catch
            {
                Interlocked.Increment(ref failedCount);
                return false;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Log.Debug($"ImageCacheService: Batch download complete. Success: {successCount}, Failed: {failedCount}, Cache hits: {cacheHitCount}, Duration: {stopwatch.ElapsedMilliseconds}ms");

        return new BatchDownloadResult
        {
            TotalRequested = urlList.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            CacheHitCount = cacheHitCount,
            Duration = stopwatch.Elapsed
        };
    }

    public string OptimizeSteamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        // Steam CDN URL patterns:
        // https://steamcdn-a.akamaihd.net/steam/apps/{appid}/capsule_184x69.jpg
        // https://cdn.akamai.steamstatic.com/steam/apps/{appid}/capsule_184x69.jpg
        // https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{appid}/{hash}.jpg
        
        // Prefer akamaihd.net (Akamai CDN) as it's optimized for global delivery
        if (url.Contains("cdn.akamai.steamstatic.com"))
        {
            url = url.Replace("cdn.akamai.steamstatic.com", "steamcdn-a.akamaihd.net");
        }
        
        // Use optimal image size for capsule images
        // If we're requesting capsule_sm_120.jpg, we can upgrade to capsule_184x69.jpg for better quality
        if (url.Contains("capsule_sm_120.jpg"))
        {
            url = url.Replace("capsule_sm_120.jpg", "capsule_184x69.jpg");
        }

        return url;
    }

    private async Task SaveMetadataAsync(string url, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var metadata = new CacheMetadata
        {
            ETag = response.Headers.ETag?.Tag?.Trim('"'),
            LastModified = response.Content.Headers.LastModified,
            CachedAt = DateTime.UtcNow
        };

        _metadataCache[url] = metadata;

        // Save to disk
        var metadataPath = GetMetadataPath(url);
        try
        {
            var json = JsonSerializer.Serialize(metadata, _jsonOptions);
            await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to save cache metadata: {ex.Message}");
        }
    }

    private void LoadMetadataCache()
    {
        try
        {
            if (!Directory.Exists(_metadataDirectory)) return;

            foreach (var file in Directory.GetFiles(_metadataDirectory, "*.meta"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var metadata = JsonSerializer.Deserialize<CacheMetadata>(json, _jsonOptions);
                    if (metadata != null)
                    {
                        // Extract URL from filename (reverse of GetMetadataPath)
                        var key = Path.GetFileNameWithoutExtension(file);
                        _metadataCache[key] = metadata;
                    }
                }
                catch
                {
                    // Skip invalid metadata files
                }
            }

            Log.Debug($"ImageCacheService: Loaded {_metadataCache.Count} metadata entries");
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to load metadata cache: {ex.Message}");
        }
    }

    private string GetMetadataPath(string url)
    {
        var hash = GetCacheKey(url);
        return Path.Combine(_metadataDirectory, $"{hash}.meta");
    }

    private static void UpdateFileAccessTime(string cachePath)
    {
        try
        {
            File.SetLastAccessTimeUtc(cachePath, DateTime.UtcNow);
        }
        catch
        {
            // Ignore access time update failures
        }
    }

    private void AddToMemoryCache(string url, byte[] data)
    {
        RemoveDeadMemoryEntries();
        // Only cache in memory if it fits
        var currentMemorySize = GetLiveMemoryCacheSize();
        
        if (currentMemorySize + data.Length <= MaxMemoryCacheSizeBytes)
        {
            _memoryCache[url] = new MemoryCacheEntry(data);
        }
        else
        {
            // Evict oldest memory entries to make room
            EvictMemoryCache(data.Length);
            _memoryCache[url] = new MemoryCacheEntry(data);
        }
    }

    private void EvictMemoryCache(long neededBytes)
    {
        RemoveDeadMemoryEntries();
        var currentSize = GetLiveMemoryCacheSize();
        var targetSize = MaxMemoryCacheSizeBytes - neededBytes;

        if (currentSize <= targetSize) return;

        var toEvict = _memoryCache
            .OrderBy(kv => kv.Value.LastAccess)
            .TakeWhile(kv =>
            {
                currentSize -= kv.Value.SizeBytes;
                return currentSize > targetSize;
            })
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in toEvict)
        {
            _memoryCache.TryRemove(key, out _);
        }
    }

    private void RemoveDeadMemoryEntries()
    {
        foreach (var entry in _memoryCache)
        {
            if (!entry.Value.DataRef.TryGetTarget(out _))
            {
                _memoryCache.TryRemove(entry.Key, out _);
            }
        }
    }

    private long GetLiveMemoryCacheSize()
    {
        long size = 0;
        foreach (var entry in _memoryCache.Values)
        {
            if (entry.DataRef.TryGetTarget(out _))
            {
                size += entry.SizeBytes;
            }
        }

        return size;
    }

    public void ClearCache()
    {
        _memoryCache.Clear();
        
        ClearCacheDirectory(_coverCacheDirectory);
        ClearCacheDirectory(_widecoverCacheDirectory);
        
        _currentCacheSize = 0;
        Log.Info("ImageCacheService: Cache cleared");
    }

    private static void ClearCacheDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Log.Debug($"Failed to delete cache file: {ex.Message}");
                }
            }
        }
    }

    public bool IsCached(string url)
    {
        if (_memoryCache.TryGetValue(url, out var cached) && cached.DataRef.TryGetTarget(out _))
        {
            return true;
        }

        var cacheDirectory = GetCacheDirectoryForUrl(url);
        var cacheKey = GetCacheKey(url);
        var cachePath = Path.Combine(cacheDirectory, cacheKey);
        return File.Exists(cachePath);
    }

    public ImageCacheStatistics GetStatistics()
    {
        RemoveDeadMemoryEntries();
        var memorySize = GetLiveMemoryCacheSize();
        
        return new ImageCacheStatistics
        {
            TotalSizeBytes = _currentCacheSize,
            FileCount = GetCacheFileCount(),
            MemoryCacheCount = _memoryCache.Count,
            MemoryCacheSizeBytes = memorySize,
            MaxSizeBytes = MaxCacheSizeBytes,
            ConditionalHits = _conditionalHits,
            ConditionalMisses = _conditionalMisses
        };
    }

    public async Task EvictOldEntriesAsync(CancellationToken cancellationToken = default)
    {
        if (_evictionInProgress) return;

        await _evictionLock.WaitAsync(cancellationToken);
        try
        {
            if (_currentCacheSize <= MaxCacheSizeBytes) return;
            
            _evictionInProgress = true;
            Log.Debug($"ImageCacheService: Starting LRU eviction. Current size: {_currentCacheSize / 1024 / 1024}MB, Max: {MaxCacheSizeBytes / 1024 / 1024}MB");

            // Get all cached files with their access times
            var files = GetAllCacheFiles()
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.LastAccessTimeUtc)
                .ToList();

            // Target: reduce to 80% of max to avoid frequent evictions
            var targetSize = (long)(MaxCacheSizeBytes * 0.8);
            var currentSize = _currentCacheSize;
            var deletedCount = 0;
            var deletedSize = 0L;

            foreach (var file in files)
            {
                if (currentSize <= targetSize)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileSize = file.Length;
                    file.Delete();
                    currentSize -= fileSize;
                    deletedSize += fileSize;
                    deletedCount++;
                }
                catch
                {
                    // Skip files that can't be deleted
                }
            }

            _currentCacheSize = currentSize;
            Log.Info($"ImageCacheService: LRU eviction complete. Deleted {deletedCount} files ({deletedSize / 1024}KB)");
        }
        finally
        {
            _evictionInProgress = false;
            _evictionLock.Release();
        }
    }

    private long CalculateDiskCacheSize()
    {
        return GetAllCacheFiles()
            .Select(f => new FileInfo(f))
            .Sum(f => f.Length);
    }

    private int GetCacheFileCount()
    {
        return GetAllCacheFiles().Count();
    }

    private IEnumerable<string> GetAllCacheFiles()
    {
        var files = Enumerable.Empty<string>();
        
        if (Directory.Exists(_coverCacheDirectory))
        {
            files = files.Concat(Directory.GetFiles(_coverCacheDirectory));
        }
        
        if (Directory.Exists(_widecoverCacheDirectory))
        {
            files = files.Concat(Directory.GetFiles(_widecoverCacheDirectory));
        }

        return files;
    }

    /// <summary>
    /// Determines the appropriate cache directory based on the URL.
    /// Header images go to Widecover, cover/capsule images go to Cover.
    /// </summary>
    private string GetCacheDirectoryForUrl(string url)
    {
        // Steam header images (e.g., header.jpg, header_292x136.jpg)
        if (url.Contains("/header") || url.Contains("_header"))
        {
            return _widecoverCacheDirectory;
        }
        
        // Default to cover cache for capsule/portrait images
        return _coverCacheDirectory;
    }

    private static string GetCacheKey(string url)
    {
        // Create a safe filename from the URL hash
        var hash = url.GetHashCode();
        var extension = Path.GetExtension(new Uri(url).LocalPath);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".bin";
        }
        return $"{hash:X8}{extension}";
    }
}
