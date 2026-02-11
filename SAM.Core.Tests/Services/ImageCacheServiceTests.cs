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

using SAM.Core.Tests.Mocks;

namespace SAM.Core.Tests.Services;

public class ImageCacheServiceTests
{
    [Fact]
    public async Task GetImageAsync_ReturnsNullForUncachedUrl()
    {
        // Arrange
        var service = new MockImageCacheService();

        // Act
        var result = await service.GetImageAsync("https://example.com/image.png");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetImageAsync_ReturnsCachedImage()
    {
        // Arrange
        var service = new MockImageCacheService();
        var testImage = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        service.SetImage("https://example.com/image.png", testImage);

        // Act
        var result = await service.GetImageAsync("https://example.com/image.png");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testImage, result);
    }

    [Fact]
    public void IsCached_ReturnsFalseForUncachedUrl()
    {
        // Arrange
        var service = new MockImageCacheService();

        // Act
        var result = service.IsCached("https://example.com/image.png");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCached_ReturnsTrueForCachedUrl()
    {
        // Arrange
        var service = new MockImageCacheService();
        service.SetImage("https://example.com/image.png", [0x89, 0x50, 0x4E, 0x47]);

        // Act
        var result = service.IsCached("https://example.com/image.png");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ClearCache_RemovesAllCachedImages()
    {
        // Arrange
        var service = new MockImageCacheService();
        service.SetImage("https://example.com/image1.png", [1, 2, 3]);
        service.SetImage("https://example.com/image2.png", [4, 5, 6]);

        // Act
        service.ClearCache();

        // Assert
        Assert.False(service.IsCached("https://example.com/image1.png"));
        Assert.False(service.IsCached("https://example.com/image2.png"));
    }
}
