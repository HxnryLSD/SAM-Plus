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
/// Service for managing application localization.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current language code (e.g., "en", "de").
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Gets a list of available languages.
    /// </summary>
    IReadOnlyList<LanguageInfo> AvailableLanguages { get; }

    /// <summary>
    /// Sets the application language.
    /// </summary>
    /// <param name="languageCode">The language code (e.g., "en", "de").</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string or the key if not found.</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string with format parameters.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Event raised when the language changes.
    /// </summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}

/// <summary>
/// Information about a supported language.
/// </summary>
public record LanguageInfo(string Code, string NativeName, string EnglishName);

/// <summary>
/// Event args for language change events.
/// </summary>
public class LanguageChangedEventArgs : EventArgs
{
    public string OldLanguage { get; init; } = "";
    public string NewLanguage { get; init; } = "";
}
