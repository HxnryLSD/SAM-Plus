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
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;

namespace SAM.API
{
    /// <summary>
    /// Object pool for Bitmap instances to reduce GC pressure during icon downloads.
    /// Reuses bitmaps of the same size instead of creating new ones.
    /// </summary>
    public sealed class BitmapPool : IDisposable
    {
        private readonly ConcurrentDictionary<(int Width, int Height), ConcurrentBag<Bitmap>> _pools = new();
        private readonly int _maxPoolSize;
        private bool _disposed;

        /// <summary>
        /// Gets the default shared instance of the bitmap pool.
        /// </summary>
        public static BitmapPool Shared { get; } = new BitmapPool(maxPoolSize: 50);

        /// <summary>
        /// Creates a new BitmapPool with the specified maximum pool size per dimension.
        /// </summary>
        /// <param name="maxPoolSize">Maximum number of bitmaps to keep per size.</param>
        public BitmapPool(int maxPoolSize = 50)
        {
            _maxPoolSize = maxPoolSize;
        }

        /// <summary>
        /// Rents a bitmap of the specified size from the pool, or creates a new one if none available.
        /// </summary>
        /// <param name="width">Width of the bitmap.</param>
        /// <param name="height">Height of the bitmap.</param>
        /// <returns>A bitmap of the specified size.</returns>
        public Bitmap Rent(int width, int height)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BitmapPool));

            var key = (width, height);
            var pool = _pools.GetOrAdd(key, _ => new ConcurrentBag<Bitmap>());

            if (pool.TryTake(out var bitmap))
            {
                // Clear the bitmap before returning
                ClearBitmap(bitmap);
                return bitmap;
            }

            // No bitmap available, create a new one
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Returns a bitmap to the pool for reuse.
        /// </summary>
        /// <param name="bitmap">The bitmap to return.</param>
        public void Return(Bitmap bitmap)
        {
            if (_disposed || bitmap == null) return;

            var key = (bitmap.Width, bitmap.Height);
            var pool = _pools.GetOrAdd(key, _ => new ConcurrentBag<Bitmap>());

            // Only keep up to maxPoolSize bitmaps per size
            if (pool.Count < _maxPoolSize)
            {
                pool.Add(bitmap);
            }
            else
            {
                // Pool is full, dispose this bitmap
                bitmap.Dispose();
            }
        }

        /// <summary>
        /// Creates a clone of the source bitmap using a pooled bitmap.
        /// </summary>
        /// <param name="source">The source bitmap to clone.</param>
        /// <returns>A pooled bitmap containing a copy of the source.</returns>
        public Bitmap CloneToPooled(Bitmap source)
        {
            if (source == null) return null;

            var pooled = Rent(source.Width, source.Height);
            using (var g = Graphics.FromImage(pooled))
            {
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }
            return pooled;
        }

        /// <summary>
        /// Clears all pooled bitmaps and releases memory.
        /// </summary>
        public void Clear()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.TryTake(out var bitmap))
                {
                    bitmap.Dispose();
                }
            }
            _pools.Clear();
        }

        /// <summary>
        /// Gets statistics about the pool.
        /// </summary>
        public (int TotalBitmaps, int TotalPools) GetStats()
        {
            int totalBitmaps = 0;
            int totalPools = _pools.Count;
            foreach (var pool in _pools.Values)
            {
                totalBitmaps += pool.Count;
            }
            return (totalBitmaps, totalPools);
        }

        private static void ClearBitmap(Bitmap bitmap)
        {
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }
    }
}
