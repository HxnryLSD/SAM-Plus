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
using SAM.Core.Tests.Mocks;

namespace SAM.Core.Tests.Services;

public class LocalizationServiceTests
{
    [Fact]
    public void GetString_ReturnsValueForCurrentLanguage()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);

        // Act
        var result = service.GetString("Nav.Games");

        // Assert
        Assert.Equal("Games", result);
    }

    [Fact]
    public void GetString_ReturnsValueForGermanLanguage()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);
        service.SetLanguage("de");

        // Act
        var result = service.GetString("Nav.Games");

        // Assert
        Assert.Equal("Spiele", result);
    }

    [Fact]
    public void GetString_FallsBackToEnglishWhenMissingInCurrentLanguage()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "de" };
        var service = new LocalizationService(settings);
        var stringsField = typeof(LocalizationService)
            .GetField("_strings", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(stringsField);

        var strings = (Dictionary<string, Dictionary<string, string>>)stringsField!.GetValue(null)!;
        var englishStrings = strings["en"];
        var germanStrings = strings["de"];
        const string key = "Test.OnlyEn";

        var hasEnglish = englishStrings.ContainsKey(key);
        var hasGerman = germanStrings.ContainsKey(key);

        if (!hasEnglish)
        {
            englishStrings[key] = "Only EN";
        }

        if (hasGerman)
        {
            germanStrings.Remove(key);
        }

        try
        {
            // Act
            var result = service.GetString(key);

            // Assert
            Assert.Equal("Only EN", result);
        }
        finally
        {
            if (!hasEnglish)
            {
                englishStrings.Remove(key);
            }

            if (hasGerman)
            {
                germanStrings[key] = "Nur DE";
            }
        }
    }

    [Fact]
    public void GetString_ReturnsKeyWhenMissingInBothLanguages()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);

        // Act
        var result = service.GetString("Missing.Key");

        // Assert
        Assert.Equal("Missing.Key", result);
    }

    [Fact]
    public void GetString_WithFormatParameters_ReturnsFormattedString()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);

        // Act
        var result = service.GetString("AchievementManager.ProtectedMessage", 2, 10);

        // Assert
        Assert.Equal("2 of 10 achievements are protected and cannot be modified.", result);
    }

    [Fact]
    public void GetString_WithInvalidFormat_ReturnsFormatString()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);

        // Act
        var result = service.GetString("GamePicker.GameCount");

        // Assert
        Assert.Equal("{0} games", result);
    }

    [Fact]
    public void SetLanguage_ValidLanguage_UpdatesSettingsAndRaisesEvent()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);
        LanguageChangedEventArgs? args = null;
        service.LanguageChanged += (_, e) => args = e;

        // Act
        service.SetLanguage("de");

        // Assert
        Assert.Equal("de", service.CurrentLanguage);
        Assert.Equal("de", settings.Language);
        Assert.True(settings.SaveAsyncCalled);
        Assert.NotNull(args);
        Assert.Equal("en", args!.OldLanguage);
        Assert.Equal("de", args.NewLanguage);
    }

    [Fact]
    public void SetLanguage_InvalidLanguage_DoesNotChangeLanguage()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);
        var eventRaised = false;
        service.LanguageChanged += (_, _) => eventRaised = true;

        // Act
        service.SetLanguage("fr");

        // Assert
        Assert.Equal("en", service.CurrentLanguage);
        Assert.False(settings.SaveAsyncCalled);
        Assert.False(eventRaised);
    }

    [Fact]
    public void AvailableLanguages_ReturnsEnglishAndGerman()
    {
        // Arrange
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);

        // Act
        var languages = service.AvailableLanguages.Select(l => l.Code).ToArray();

        // Assert
        Assert.Equal(["en", "de"], languages);
    }

    [Fact]
    public void LocGet_ReturnsKeyBeforeInitialize()
    {
        // Arrange
        ResetLocService();

        // Act
        var result = Loc.Get("Nav.Games");

        // Assert
        Assert.Equal("Nav.Games", result);
    }

    [Fact]
    public void LocGet_ReturnsLocalizedStringAfterInitialize()
    {
        // Arrange
        ResetLocService();
        var settings = new MockSettingsService { Language = "en" };
        var service = new LocalizationService(settings);
        Loc.Initialize(service);

        // Act
        var result = Loc.Get("Nav.Games");

        // Assert
        Assert.Equal("Games", result);
    }

    private static void ResetLocService()
    {
        var field = typeof(Loc).GetField("_service", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, null);
    }
}
