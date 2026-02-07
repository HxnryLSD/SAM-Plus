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
using System.Drawing;
using System.Drawing.Imaging;

namespace SAM.API
{
    /// <summary>
    /// Helper class for icon manipulation and dark theme compatibility.
    /// </summary>
    public static class IconHelper
    {
        /// <summary>
        /// Adjusts icon brightness for better visibility on dark backgrounds.
        /// </summary>
        /// <param name="original">Original bitmap</param>
        /// <param name="brightnessBoost">Brightness increase (0-100)</param>
        /// <returns>Brightened bitmap for dark theme</returns>
        public static Bitmap AdjustForDarkTheme(Bitmap original, int brightnessBoost = 30)
        {
            if (original == null) return null;

            var result = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
            
            float factor = 1 + (brightnessBoost / 100f);
            
            // Color matrix for brightness adjustment
            float[][] matrixItems = 
            {
                new float[] { factor, 0, 0, 0, 0 },
                new float[] { 0, factor, 0, 0, 0 },
                new float[] { 0, 0, factor, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            };

            var colorMatrix = new ColorMatrix(matrixItems);
            var imageAttributes = new ImageAttributes();
            imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            using var g = Graphics.FromImage(result);
            g.DrawImage(
                original,
                new Rectangle(0, 0, original.Width, original.Height),
                0, 0, original.Width, original.Height,
                GraphicsUnit.Pixel,
                imageAttributes);

            return result;
        }

        /// <summary>
        /// Tints an icon with a specific color while preserving transparency.
        /// </summary>
        /// <param name="original">Original bitmap</param>
        /// <param name="tintColor">Color to tint with</param>
        /// <param name="intensity">Tint intensity (0.0-1.0)</param>
        /// <returns>Tinted bitmap</returns>
        public static Bitmap TintIcon(Bitmap original, Color tintColor, float intensity = 0.3f)
        {
            if (original == null) return null;

            var result = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    var pixel = original.GetPixel(x, y);
                    
                    if (pixel.A > 0) // Only modify non-transparent pixels
                    {
                        int r = (int)(pixel.R + (tintColor.R - pixel.R) * intensity);
                        int g = (int)(pixel.G + (tintColor.G - pixel.G) * intensity);
                        int b = (int)(pixel.B + (tintColor.B - pixel.B) * intensity);
                        
                        r = Math.Clamp(r, 0, 255);
                        g = Math.Clamp(g, 0, 255);
                        b = Math.Clamp(b, 0, 255);
                        
                        result.SetPixel(x, y, Color.FromArgb(pixel.A, r, g, b));
                    }
                    else
                    {
                        result.SetPixel(x, y, pixel);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a themed icon based on current theme settings.
        /// </summary>
        /// <param name="original">Original bitmap</param>
        /// <returns>Theme-adjusted bitmap</returns>
        public static Bitmap GetThemedIcon(Bitmap original)
        {
            if (original == null) return null;

            if (ThemeManager.IsDarkMode)
            {
                // Brighten icons slightly for dark theme
                return AdjustForDarkTheme(original, 20);
            }

            return new Bitmap(original);
        }
    }
}
