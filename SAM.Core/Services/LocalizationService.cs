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

using System.Globalization;

namespace SAM.Core.Services;

/// <summary>
/// Core localization service with built-in string dictionaries.
/// Can be extended by UI projects to use platform-specific resource systems.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ISettingsService _settingsService;
    private string _currentLanguage = "en";
    
    private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            // App
            ["App.Title"] = "Steam Achievement Manager",
            
            // Navigation
            ["Nav.Games"] = "Games",
            ["Nav.Diagnostics"] = "Diagnostics",
            ["Nav.About"] = "About",
            ["Nav.Settings"] = "Settings",
            
            // Game Picker
            ["GamePicker.Title"] = "Games",
            ["GamePicker.SearchPlaceholder"] = "Search game...",
            ["GamePicker.Refresh"] = "Refresh",
            ["GamePicker.Filter.All"] = "All",
            ["GamePicker.Filter.GamesOnly"] = "Games only",
            ["GamePicker.Filter.ModsOnly"] = "Mods only",
            ["GamePicker.Filter.DlcsOnly"] = "DLCs only",
            ["GamePicker.Filter.DemosOnly"] = "Demos only",
            ["GamePicker.Loading"] = "Loading games...",
            ["GamePicker.InitializingSteam"] = "Initializing Steam...",
            ["GamePicker.Refreshing"] = "Updating library...",
            ["GamePicker.NoGames"] = "No games found",
            ["GamePicker.EnsureSteamRunning"] = "Make sure Steam is running and you are logged in.",
            ["GamePicker.GameCount"] = "{0} games",
            
            // Achievement Manager
            ["AchievementManager.Achievements"] = "Achievements",
            ["AchievementManager.SearchPlaceholder"] = "Search achievement...",
            ["AchievementManager.Refresh"] = "Refresh",
            ["AchievementManager.Save"] = "Save",
            ["AchievementManager.UnlockAll"] = "Unlock All",
            ["AchievementManager.LockAll"] = "Lock All",
            ["AchievementManager.Invert"] = "Invert",
            ["AchievementManager.Statistics"] = "Statistics",
            ["AchievementManager.ResetAll"] = "Reset all stats",
            ["AchievementManager.Filter.All"] = "All",
            ["AchievementManager.Filter.Unlocked"] = "Unlocked",
            ["AchievementManager.Filter.Locked"] = "Locked",
            ["AchievementManager.Filter.Modified"] = "Modified",
            ["AchievementManager.ProtectedTitle"] = "Protected Achievements",
            ["AchievementManager.ProtectedMessage"] = "{0} of {1} achievements are protected and cannot be modified.",
            ["AchievementManager.SaveSuccess"] = "Changes saved successfully",
            ["AchievementManager.SaveFailed"] = "Failed to save changes",
            ["AchievementManager.Hidden"] = "Hidden",
            ["AchievementManager.Protected"] = "Protected",
            
            // Statistics
            ["Statistics.Title"] = "Statistics",
            ["Statistics.Name"] = "Name",
            ["Statistics.Value"] = "Value",
            ["Statistics.NoStats"] = "No statistics available",
            
            // Settings
            ["Settings.Title"] = "Settings",
            ["Settings.Appearance"] = "Appearance",
            ["Settings.AppTheme"] = "App theme",
            ["Settings.AppThemeDescription"] = "Choose between Light, Dark, or System setting",
            ["Settings.Theme.System"] = "System",
            ["Settings.Theme.Light"] = "Light",
            ["Settings.Theme.Dark"] = "Dark",
            ["Settings.AccentColor"] = "Accent color",
            ["Settings.AccentColorDescription"] = "Use the system accent color or choose a custom one",
            ["Settings.AccentColor.System"] = "System",
            ["Settings.AccentColor.Custom"] = "Custom",
            ["Settings.Language"] = "Language",
            ["Settings.LanguageDescription"] = "Select your preferred language",
            ["Settings.Behavior"] = "Behavior",
            ["Settings.WarnUnsaved"] = "Warn on unsaved changes",
            ["Settings.WarnUnsavedDescription"] = "Shows a warning when leaving with unsaved changes",
            ["Settings.ShowHidden"] = "Show hidden achievements",
            ["Settings.ShowHiddenDescription"] = "Also displays hidden achievements in the list",
            ["Settings.SyncSettings"] = "Sync settings between apps",
            ["Settings.SyncSettingsDescription"] = "Share settings between SAM.WinUI and SAM.Manager",
            ["Settings.AutoUpdate"] = "Check for updates on start",
            ["Settings.AutoUpdateDescription"] = "Checks GitHub for new releases when the app starts",
            ["Settings.Data"] = "Data",
            ["Settings.ForceFetch"] = "Sync Library Data",
            ["Settings.ForceFetchDescription"] = "Loads achievement counts and DRM status for all games. This may take a few minutes.",
            ["Settings.ForceFetchButton"] = "Sync Now",
            ["Settings.Cancel"] = "Cancel",
            ["Settings.RestartRequired"] = "Restart Required",
            ["Settings.RestartRequiredMessage"] = "Please restart the application to apply language changes.",
            
            // About
            ["About.Title"] = "About",
            ["About.Version"] = "Version",
            ["About.Description"] = "A tool for managing Steam achievements.",
            ["About.DescriptionLong"] = "With Steam Achievement Manager you can manage, unlock and lock achievements for Steam games. This tool is intended for personal use only.",
            ["About.Disclaimer"] = "This tool is provided as-is. Use at your own risk.",
            ["About.OpenSource"] = "Open Source",
            ["About.ViewOnGitHub"] = "View on GitHub",
            ["About.DevelopedBy"] = "Developed by",
            ["About.OriginalDeveloper"] = "Original Developer",
            ["About.WinUI3Contributor"] = "WinUI 3 Rewrite, Modernization & Features",
            ["About.Links"] = "Links",
            ["About.ReportBug"] = "Report Bug",
            ["About.License"] = "License",
            
            // Settings (additional)
            ["Settings.AboutSection"] = "About",
            
            // Diagnostics
            ["Diagnostics.Title"] = "Diagnostics",
            ["Diagnostics.Copy"] = "Copy",
            ["Diagnostics.Refresh"] = "Refresh",
            ["Diagnostics.CopyTooltip"] = "Copy all info to clipboard",
            ["Diagnostics.RefreshTooltip"] = "Refresh diagnostic info",
            ["Diagnostics.System"] = "System",
            ["Diagnostics.OperatingSystem"] = "Operating System:",
            ["Diagnostics.DotNetRuntime"] = ".NET Runtime:",
            ["Diagnostics.WindowsAppSDK"] = "Windows App SDK:",
            ["Diagnostics.AppVersion"] = "App Version:",
            ["Diagnostics.ProcessArchitecture"] = "Process Architecture:",
            ["Diagnostics.MemoryUsage"] = "Memory Usage:",
            ["Diagnostics.ProcessId"] = "Process ID:",
            ["Diagnostics.Steam"] = "Steam",
            ["Diagnostics.ConnectionStatus"] = "Connection Status:",
            ["Diagnostics.SteamUserId"] = "Steam User ID:",
            ["Diagnostics.GameCount"] = "Game Count:",
            ["Diagnostics.SteamInstallPath"] = "Steam Installation Path:",
            ["Diagnostics.SteamApiStatus"] = "steam_api.dll Status:",
            ["Diagnostics.Storage"] = "Storage",
            ["Diagnostics.AppDataFolder"] = "App Data Folder:",
            ["Diagnostics.SettingsFile"] = "Settings File:",
            ["Diagnostics.ImageCache"] = "Image Cache:",
            ["Diagnostics.CacheSize"] = "Cache Size:",
            ["Diagnostics.LogFile"] = "Log File",
            ["Diagnostics.CurrentLogFile"] = "Current Log File:",
            ["Diagnostics.RecentLogEntries"] = "Recent Log Entries:",
            ["Diagnostics.OpenLogFile"] = "Open Log File",
            ["Diagnostics.RefreshLog"] = "Refresh Log",
            ["Diagnostics.Migration"] = "Legacy Migration",
            ["Diagnostics.LegacyDataPresent"] = "Legacy Data Present:",
            ["Diagnostics.MigrationComplete"] = "Migration Complete:",
            ["Diagnostics.MigrateData"] = "Migrate Data",
            ["Diagnostics.Actions"] = "Actions",
            ["Diagnostics.ClearImageCache"] = "Clear Image Cache",
            ["Diagnostics.ResetSettings"] = "Reset Settings",
            ["Diagnostics.OpenAppFolder"] = "Open App Folder",
            ["Diagnostics.OpenInstallFolder"] = "Open Install Folder",
            ["Diagnostics.Tests"] = "Tests",
            ["Diagnostics.TestNotification"] = "Test Notification",
            ["Diagnostics.ToggleTheme"] = "Toggle Theme",
            ["Diagnostics.TestSteamConnection"] = "Test Steam Connection",
            ["Diagnostics.SteamStatus"] = "Steam Status",
            ["Diagnostics.Connected"] = "Connected",
            ["Diagnostics.NotConnected"] = "Not connected",
            ["Diagnostics.SteamId"] = "Steam ID",
            ["Diagnostics.AppId"] = "Current App ID",
            ["Diagnostics.Logs"] = "Logs",
            ["Diagnostics.OpenLogFolder"] = "Open log folder",
            ["Diagnostics.ClearCache"] = "Clear image cache",
            
            // Common
            ["Common.Yes"] = "Yes",
            ["Common.No"] = "No",
            ["Common.OK"] = "OK",
            ["Common.Cancel"] = "Cancel",
            ["Common.Close"] = "Close",
            ["Common.Error"] = "Error",
            ["Common.Warning"] = "Warning",
            ["Common.Success"] = "Success",
            ["Common.Loading"] = "Loading...",
            ["Common.Retry"] = "Retry",
            
            // Errors
            ["Error.SteamNotRunning"] = "Steam is not running. Please start Steam and try again.",
            ["Error.GameNotOwned"] = "You do not own this game.",
            ["Error.SchemaLoadFailed"] = "Failed to load game schema. The game may not have achievements.",
            ["Error.ConnectionFailed"] = "Connection to Steam failed.",
            ["Error.Timeout"] = "The request timed out. Please try again.",
        },
        
        ["de"] = new Dictionary<string, string>
        {
            // App
            ["App.Title"] = "Steam Achievement Manager",
            
            // Navigation
            ["Nav.Games"] = "Spiele",
            ["Nav.Diagnostics"] = "Diagnose",
            ["Nav.About"] = "Info",
            ["Nav.Settings"] = "Einstellungen",
            
            // Game Picker
            ["GamePicker.Title"] = "Spiele",
            ["GamePicker.SearchPlaceholder"] = "Spiel suchen...",
            ["GamePicker.Refresh"] = "Aktualisieren",
            ["GamePicker.Filter.All"] = "Alle",
            ["GamePicker.Filter.GamesOnly"] = "Nur Spiele",
            ["GamePicker.Filter.ModsOnly"] = "Nur Mods",
            ["GamePicker.Filter.DlcsOnly"] = "Nur DLCs",
            ["GamePicker.Filter.DemosOnly"] = "Nur Demos",
            ["GamePicker.Loading"] = "Spiele werden geladen...",
            ["GamePicker.InitializingSteam"] = "Steam wird initialisiert...",
            ["GamePicker.Refreshing"] = "Bibliothek wird aktualisiert...",
            ["GamePicker.NoGames"] = "Keine Spiele gefunden",
            ["GamePicker.EnsureSteamRunning"] = "Stelle sicher, dass Steam läuft und du eingeloggt bist.",
            ["GamePicker.GameCount"] = "{0} Spiele",
            
            // Achievement Manager
            ["AchievementManager.Achievements"] = "Erfolge",
            ["AchievementManager.SearchPlaceholder"] = "Erfolg suchen...",
            ["AchievementManager.Refresh"] = "Aktualisieren",
            ["AchievementManager.Save"] = "Speichern",
            ["AchievementManager.UnlockAll"] = "Alle freischalten",
            ["AchievementManager.LockAll"] = "Alle sperren",
            ["AchievementManager.Invert"] = "Invertieren",
            ["AchievementManager.Statistics"] = "Statistiken",
            ["AchievementManager.ResetAll"] = "Alle Stats zurücksetzen",
            ["AchievementManager.Filter.All"] = "Alle",
            ["AchievementManager.Filter.Unlocked"] = "Freigeschaltet",
            ["AchievementManager.Filter.Locked"] = "Gesperrt",
            ["AchievementManager.Filter.Modified"] = "Geändert",
            ["AchievementManager.ProtectedTitle"] = "Geschützte Erfolge",
            ["AchievementManager.ProtectedMessage"] = "{0} von {1} Erfolgen sind geschützt und können nicht bearbeitet werden.",
            ["AchievementManager.SaveSuccess"] = "Änderungen erfolgreich gespeichert",
            ["AchievementManager.SaveFailed"] = "Änderungen konnten nicht gespeichert werden",
            ["AchievementManager.Hidden"] = "Versteckt",
            ["AchievementManager.Protected"] = "Geschützt",
            
            // Statistics
            ["Statistics.Title"] = "Statistiken",
            ["Statistics.Name"] = "Name",
            ["Statistics.Value"] = "Wert",
            ["Statistics.NoStats"] = "Keine Statistiken verfügbar",
            
            // Settings
            ["Settings.Title"] = "Einstellungen",
            ["Settings.Appearance"] = "Darstellung",
            ["Settings.AppTheme"] = "App-Design",
            ["Settings.AppThemeDescription"] = "Wähle zwischen Hell, Dunkel oder Systemeinstellung",
            ["Settings.Theme.System"] = "System",
            ["Settings.Theme.Light"] = "Hell",
            ["Settings.Theme.Dark"] = "Dunkel",
            ["Settings.AccentColor"] = "Akzentfarbe",
            ["Settings.AccentColorDescription"] = "Systemakzent verwenden oder eigene Farbe wählen",
            ["Settings.AccentColor.System"] = "System",
            ["Settings.AccentColor.Custom"] = "Benutzerdefiniert",
            ["Settings.Language"] = "Sprache",
            ["Settings.LanguageDescription"] = "Wähle deine bevorzugte Sprache",
            ["Settings.Behavior"] = "Verhalten",
            ["Settings.WarnUnsaved"] = "Warnung bei ungespeicherten Änderungen",
            ["Settings.WarnUnsavedDescription"] = "Zeigt eine Warnung beim Verlassen mit ungespeicherten Änderungen",
            ["Settings.ShowHidden"] = "Versteckte Erfolge anzeigen",
            ["Settings.ShowHiddenDescription"] = "Zeigt auch versteckte Erfolge in der Liste an",
            ["Settings.SyncSettings"] = "Einstellungen zwischen Apps synchronisieren",
            ["Settings.SyncSettingsDescription"] = "Teilt Einstellungen zwischen SAM.WinUI und SAM.Manager",
            ["Settings.AutoUpdate"] = "Beim Start nach Updates suchen",
            ["Settings.AutoUpdateDescription"] = "Prueft GitHub beim Start auf neue Releases",
            ["Settings.Data"] = "Daten",
            ["Settings.ForceFetch"] = "Bibliothek synchronisieren",
            ["Settings.ForceFetchDescription"] = "Lädt Achievement-Anzahlen und DRM-Status für alle Spiele. Dies kann einige Minuten dauern.",
            ["Settings.ForceFetchButton"] = "Jetzt synchronisieren",
            ["Settings.Cancel"] = "Abbrechen",
            ["Settings.RestartRequired"] = "Neustart erforderlich",
            ["Settings.RestartRequiredMessage"] = "Bitte starte die Anwendung neu, um die Sprachänderungen zu übernehmen.",
            
            // About
            ["About.Title"] = "Info",
            ["About.Version"] = "Version",
            ["About.Description"] = "Ein Tool zur Verwaltung von Steam-Erfolgen.",
            ["About.DescriptionLong"] = "Mit dem Steam Achievement Manager kannst du Achievements für Steam-Spiele verwalten, freischalten und sperren. Dieses Tool ist nur für den persönlichen Gebrauch gedacht.",
            ["About.Disclaimer"] = "Dieses Tool wird ohne Gewähr bereitgestellt. Verwendung auf eigene Gefahr.",
            ["About.OpenSource"] = "Open Source",
            ["About.ViewOnGitHub"] = "Auf GitHub ansehen",
            ["About.DevelopedBy"] = "Entwickelt von",
            ["About.OriginalDeveloper"] = "Original-Entwickler",
            ["About.WinUI3Contributor"] = "WinUI 3 Neuentwicklung, Modernisierung & Features",
            ["About.Links"] = "Links",
            ["About.ReportBug"] = "Fehler melden",
            ["About.License"] = "Lizenz",
            
            // Settings (additional)
            ["Settings.AboutSection"] = "Über",
            
            // Diagnostics
            ["Diagnostics.Title"] = "Diagnose",
            ["Diagnostics.Copy"] = "Kopieren",
            ["Diagnostics.Refresh"] = "Aktualisieren",
            ["Diagnostics.CopyTooltip"] = "Alle Infos in Zwischenablage kopieren",
            ["Diagnostics.RefreshTooltip"] = "Diagnose-Infos aktualisieren",
            ["Diagnostics.System"] = "System",
            ["Diagnostics.OperatingSystem"] = "Betriebssystem:",
            ["Diagnostics.DotNetRuntime"] = ".NET Runtime:",
            ["Diagnostics.WindowsAppSDK"] = "Windows App SDK:",
            ["Diagnostics.AppVersion"] = "App Version:",
            ["Diagnostics.ProcessArchitecture"] = "Prozess-Architektur:",
            ["Diagnostics.MemoryUsage"] = "Speicherverbrauch:",
            ["Diagnostics.ProcessId"] = "Prozess-ID:",
            ["Diagnostics.Steam"] = "Steam",
            ["Diagnostics.ConnectionStatus"] = "Verbindungsstatus:",
            ["Diagnostics.SteamUserId"] = "Steam User ID:",
            ["Diagnostics.GameCount"] = "Anzahl Spiele:",
            ["Diagnostics.SteamInstallPath"] = "Steam-Installationspfad:",
            ["Diagnostics.SteamApiStatus"] = "steam_api.dll Status:",
            ["Diagnostics.Storage"] = "Speicher",
            ["Diagnostics.AppDataFolder"] = "App-Datenordner:",
            ["Diagnostics.SettingsFile"] = "Einstellungsdatei:",
            ["Diagnostics.ImageCache"] = "Bild-Cache:",
            ["Diagnostics.CacheSize"] = "Cache-Größe:",
            ["Diagnostics.LogFile"] = "Log-Datei",
            ["Diagnostics.CurrentLogFile"] = "Aktuelle Log-Datei:",
            ["Diagnostics.RecentLogEntries"] = "Letzte Log-Einträge:",
            ["Diagnostics.OpenLogFile"] = "Log-Datei öffnen",
            ["Diagnostics.RefreshLog"] = "Log aktualisieren",
            ["Diagnostics.Migration"] = "Legacy-Migration",
            ["Diagnostics.LegacyDataPresent"] = "Legacy-Daten vorhanden:",
            ["Diagnostics.MigrationComplete"] = "Migration abgeschlossen:",
            ["Diagnostics.MigrateData"] = "Daten migrieren",
            ["Diagnostics.Actions"] = "Aktionen",
            ["Diagnostics.ClearImageCache"] = "Bildercache leeren",
            ["Diagnostics.ResetSettings"] = "Einstellungen zurücksetzen",
            ["Diagnostics.OpenAppFolder"] = "App-Ordner öffnen",
            ["Diagnostics.OpenInstallFolder"] = "Installationsordner öffnen",
            ["Diagnostics.Tests"] = "Tests",
            ["Diagnostics.TestNotification"] = "Test-Benachrichtigung",
            ["Diagnostics.ToggleTheme"] = "Theme wechseln",
            ["Diagnostics.TestSteamConnection"] = "Steam-Verbindung testen",
            ["Diagnostics.SteamStatus"] = "Steam-Status",
            ["Diagnostics.Connected"] = "Verbunden",
            ["Diagnostics.NotConnected"] = "Nicht verbunden",
            ["Diagnostics.SteamId"] = "Steam-ID",
            ["Diagnostics.AppId"] = "Aktuelle App-ID",
            ["Diagnostics.Logs"] = "Protokolle",
            ["Diagnostics.OpenLogFolder"] = "Protokollordner öffnen",
            ["Diagnostics.ClearCache"] = "Bildercache leeren",
            
            // Common
            ["Common.Yes"] = "Ja",
            ["Common.No"] = "Nein",
            ["Common.OK"] = "OK",
            ["Common.Cancel"] = "Abbrechen",
            ["Common.Close"] = "Schließen",
            ["Common.Error"] = "Fehler",
            ["Common.Warning"] = "Warnung",
            ["Common.Success"] = "Erfolg",
            ["Common.Loading"] = "Wird geladen...",
            ["Common.Retry"] = "Erneut versuchen",
            
            // Errors
            ["Error.SteamNotRunning"] = "Steam läuft nicht. Bitte starte Steam und versuche es erneut.",
            ["Error.GameNotOwned"] = "Du besitzt dieses Spiel nicht.",
            ["Error.SchemaLoadFailed"] = "Spielschema konnte nicht geladen werden. Das Spiel hat möglicherweise keine Erfolge.",
            ["Error.ConnectionFailed"] = "Verbindung zu Steam fehlgeschlagen.",
            ["Error.Timeout"] = "Die Anfrage ist abgelaufen. Bitte versuche es erneut.",
        }
    };

    public static readonly IReadOnlyList<LanguageInfo> SupportedLanguages =
    [
        new LanguageInfo("en", "English", "English"),
        new LanguageInfo("de", "Deutsch", "German")
    ];

    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        // Load language from settings
        var savedLanguage = _settingsService.Language;
        if (!string.IsNullOrEmpty(savedLanguage) && _strings.ContainsKey(savedLanguage))
        {
            _currentLanguage = savedLanguage;
        }
        else
        {
            // Try to detect system language
            var systemCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (_strings.ContainsKey(systemCulture))
            {
                _currentLanguage = systemCulture;
            }
        }
    }

    public string CurrentLanguage => _currentLanguage;

    public IReadOnlyList<LanguageInfo> AvailableLanguages => SupportedLanguages;

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || !_strings.ContainsKey(languageCode))
        {
            Log.Warn($"Unsupported language: {languageCode}");
            return;
        }

        if (_currentLanguage == languageCode)
        {
            return;
        }

        var oldLanguage = _currentLanguage;
        _currentLanguage = languageCode;
        
        // Save to settings
        _settingsService.Language = languageCode;
        _ = _settingsService.SaveAsync();
        
        Log.Info($"Language changed from {oldLanguage} to {languageCode}");
        
        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs
        {
            OldLanguage = oldLanguage,
            NewLanguage = languageCode
        });
    }

    public string GetString(string key)
    {
        if (_strings.TryGetValue(_currentLanguage, out var languageStrings) &&
            languageStrings.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fallback to English
        if (_strings.TryGetValue("en", out var englishStrings) &&
            englishStrings.TryGetValue(key, out var englishValue))
        {
            return englishValue;
        }

        // Return key as fallback
        Log.Warn($"Missing localization key: {key}");
        return key;
    }

    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return format;
        }
    }
}

/// <summary>
/// Static helper for quick access to localized strings.
/// Must call Initialize() before use.
/// </summary>
public static class Loc
{
    private static ILocalizationService? _service;

    /// <summary>
    /// Initializes the static localization helper.
    /// </summary>
    public static void Initialize(ILocalizationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    public static string Get(string key) => _service?.GetString(key) ?? key;

    /// <summary>
    /// Gets a localized string with format parameters.
    /// </summary>
    public static string Get(string key, params object[] args) => _service?.GetString(key, args) ?? key;

    /// <summary>
    /// Gets the current language code.
    /// </summary>
    public static string CurrentLanguage => _service?.CurrentLanguage ?? "en";
}
