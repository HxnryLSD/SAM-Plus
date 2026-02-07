/* Copyright (c) 2024 SAM-Plus Contributors
 *
 * Icon caching service for achievement and game icons.
 * Stores downloaded icons locally to avoid repeated downloads.
 */

using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.API;

/// <summary>
/// Provides local file-based caching for downloaded icons.
/// </summary>
public static class IconCache
{
    private static readonly string CacheDirectory;
    private static readonly object _lock = new();

    static IconCache()
    {
        // Cache directory: %LOCALAPPDATA%\SAM-Plus\IconCache
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        CacheDirectory = Path.Combine(appData, "SAM-Plus", "IconCache");
        
        try
        {
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Could not create icon cache directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a cached icon if available.
    /// </summary>
    /// <param name="url">The original URL of the icon</param>
    /// <returns>The cached bitmap, or null if not cached</returns>
    public static Bitmap Get(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var filePath = GetCachePath(url);
            if (File.Exists(filePath))
            {
                // Load from disk
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new Bitmap(stream);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to load cached icon for {url}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Stores an icon in the cache.
    /// </summary>
    /// <param name="url">The original URL of the icon</param>
    /// <param name="data">The raw image data</param>
    /// <returns>The bitmap created from the data</returns>
    public static Bitmap Store(string url, byte[] data)
    {
        if (string.IsNullOrEmpty(url) || data == null || data.Length == 0)
            return null;

        try
        {
            var filePath = GetCachePath(url);
            
            // Write to disk asynchronously (fire and forget)
            Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        File.WriteAllBytes(filePath, data);
                    }
                }
                catch
                {
                    // Ignore write errors - cache is optional
                }
            });

            // Return bitmap from memory
            using var stream = new MemoryStream(data);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to cache icon for {url}: {ex.Message}");
            
            // Still try to return the bitmap
            try
            {
                using var stream = new MemoryStream(data);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Checks if an icon is already cached.
    /// </summary>
    public static bool IsCached(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return File.Exists(GetCachePath(url));
    }

    /// <summary>
    /// Clears all cached icons.
    /// </summary>
    public static void ClearAll()
    {
        try
        {
            if (Directory.Exists(CacheDirectory))
            {
                foreach (var file in Directory.GetFiles(CacheDirectory, "*.png"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
            Logger.Info("Icon cache cleared.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to clear icon cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the total size of the cache in bytes.
    /// </summary>
    public static long GetCacheSize()
    {
        try
        {
            if (!Directory.Exists(CacheDirectory)) return 0;
            
            long size = 0;
            foreach (var file in Directory.GetFiles(CacheDirectory, "*.png"))
            {
                size += new FileInfo(file).Length;
            }
            return size;
        }
        catch
        {
            return 0;
        }
    }

    private static string GetCachePath(string url)
    {
        // Create a hash-based filename from the URL
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));
        var fileName = Convert.ToHexString(hash)[..16] + ".png";
        return Path.Combine(CacheDirectory, fileName);
    }
}
