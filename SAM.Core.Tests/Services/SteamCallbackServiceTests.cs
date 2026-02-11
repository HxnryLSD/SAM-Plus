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

using System.Reflection;
using SAM.Core.Services;

namespace SAM.Core.Tests.Services;

public class SteamCallbackServiceTests
{
    [Theory]
    [InlineData(1, "Success")]
    [InlineData(2, "Generic failure")]
    [InlineData(3, "No connection")]
    [InlineData(10, "Busy")]
    [InlineData(15, "Access denied")]
    [InlineData(16, "Timeout")]
    [InlineData(84, "Rate limit exceeded")]
    [InlineData(108, "Too many pending")]
    public void TranslateResultCode_KnownCode_ReturnsExpectedMessage(int code, string expected)
    {
        // Act
        var result = SteamCallbackService.TranslateResultCode(code);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TranslateResultCode_AllKnownCodes_ReturnNonUnknownMessage()
    {
        for (int code = 1; code <= 108; code++)
        {
            if (code == 4)
            {
                continue;
            }

            var result = SteamCallbackService.TranslateResultCode(code);
            Assert.DoesNotContain("Unknown error", result, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(999)]
    public void TranslateResultCode_UnknownCode_ReturnsFallback(int code)
    {
        // Act
        var result = SteamCallbackService.TranslateResultCode(code);

        // Assert
        Assert.Equal($"Unknown error (code {code})", result);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(27)]
    [InlineData(52)]
    public void IsRetryableError_RetryableCodes_ReturnTrue(int code)
    {
        Assert.True(InvokeIsRetryableError(code));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(84)]
    public void IsRetryableError_NonRetryableCodes_ReturnFalse(int code)
    {
        Assert.False(InvokeIsRetryableError(code));
    }

    private static bool InvokeIsRetryableError(int code)
    {
        var method = typeof(SteamCallbackService)
            .GetMethod("IsRetryableError", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (bool)method!.Invoke(null, [code])!;
    }
}
