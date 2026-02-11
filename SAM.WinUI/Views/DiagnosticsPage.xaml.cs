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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SAM.Core.Services;
using SAM.WinUI.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace SAM.WinUI.Views;

/// <summary>
/// Diagnostics page for testing and troubleshooting.
/// </summary>
public sealed partial class DiagnosticsPage : Page
{
    private readonly ISettingsService _settingsService;
    private readonly ILegacyMigrationService _migrationService;
    private readonly INotificationService _notificationService;
    private readonly IThemeService _themeService;
    private readonly ISteamService _steamService;
    private string _currentLogFilePath = string.Empty;
    private string _appDataPath = string.Empty;

    public DiagnosticsPage()
    {
        _settingsService = App.GetService<ISettingsService>();
        _migrationService = App.GetService<ILegacyMigrationService>();
        _notificationService = App.GetService<INotificationService>();
        _themeService = App.GetService<IThemeService>();
        _steamService = App.GetService<ISteamService>();
        
        InitializeComponent();
        ApplyLocalization();
        LoadDiagnosticInfo();
    }

    private void ApplyLocalization()
    {
        // Page title
        PageTitleText.Text = Loc.Get("Diagnostics.Title");
        
        // Header buttons
        CopyButtonText.Text = Loc.Get("Diagnostics.Copy");
        RefreshButtonText.Text = Loc.Get("Diagnostics.Refresh");
        
        // System section
        SystemSectionText.Text = Loc.Get("Diagnostics.System");
        OsLabel.Text = Loc.Get("Diagnostics.OperatingSystem");
        DotNetLabel.Text = Loc.Get("Diagnostics.DotNetRuntime");
        WinAppSdkLabel.Text = Loc.Get("Diagnostics.WindowsAppSDK");
        AppVersionLabel.Text = Loc.Get("Diagnostics.AppVersion");
        ArchLabel.Text = Loc.Get("Diagnostics.ProcessArchitecture");
        MemoryLabel.Text = Loc.Get("Diagnostics.MemoryUsage");
        ProcessIdLabel.Text = Loc.Get("Diagnostics.ProcessId");
        
        // Steam section
        SteamSectionText.Text = Loc.Get("Diagnostics.Steam");
        ConnectionStatusLabel.Text = Loc.Get("Diagnostics.ConnectionStatus");
        SteamUserIdLabel.Text = Loc.Get("Diagnostics.SteamUserId");
        GameCountLabel.Text = Loc.Get("Diagnostics.GameCount");
        SteamPathLabel.Text = Loc.Get("Diagnostics.SteamInstallPath");
        SteamApiStatusLabel.Text = Loc.Get("Diagnostics.SteamApiStatus");
        
        // Storage section
        StorageSectionText.Text = Loc.Get("Diagnostics.Storage");
        AppDataFolderLabel.Text = Loc.Get("Diagnostics.AppDataFolder");
        SettingsFileLabel.Text = Loc.Get("Diagnostics.SettingsFile");
        ImageCacheLabel.Text = Loc.Get("Diagnostics.ImageCache");
        CacheSizeLabel.Text = Loc.Get("Diagnostics.CacheSize");
        
        // Log section
        LogFileSectionText.Text = Loc.Get("Diagnostics.LogFile");
        CurrentLogFileLabel.Text = Loc.Get("Diagnostics.CurrentLogFile");
        RecentLogEntriesLabel.Text = Loc.Get("Diagnostics.RecentLogEntries");
        OpenLogFileButtonText.Text = Loc.Get("Diagnostics.OpenLogFile");
        RefreshLogButtonText.Text = Loc.Get("Diagnostics.RefreshLog");
        
        // Migration section
        MigrationSectionText.Text = Loc.Get("Diagnostics.Migration");
        LegacyDataPresentLabel.Text = Loc.Get("Diagnostics.LegacyDataPresent");
        MigrationCompleteLabel.Text = Loc.Get("Diagnostics.MigrationComplete");
        MigrateDataButtonText.Text = Loc.Get("Diagnostics.MigrateData");
        
        // Actions section
        ActionsSectionText.Text = Loc.Get("Diagnostics.Actions");
        ClearImageCacheButtonText.Text = Loc.Get("Diagnostics.ClearImageCache");
        ResetSettingsButtonText.Text = Loc.Get("Diagnostics.ResetSettings");
        OpenAppFolderButtonText.Text = Loc.Get("Diagnostics.OpenAppFolder");
        OpenInstallFolderButtonText.Text = Loc.Get("Diagnostics.OpenInstallFolder");
        
        // Tests section
        TestsSectionText.Text = Loc.Get("Diagnostics.Tests");
        TestNotificationButtonText.Text = Loc.Get("Diagnostics.TestNotification");
        ToggleThemeButtonText.Text = Loc.Get("Diagnostics.ToggleTheme");
        TestSteamConnectionButtonText.Text = Loc.Get("Diagnostics.TestSteamConnection");
    }

    private void LoadDiagnosticInfo()
    {
        // System Information
        OsVersionText.Text = $"Windows {Environment.OSVersion.Version} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})";
        DotNetVersionText.Text = $".NET {Environment.Version}";
        WinAppSdkVersionText.Text = GetWindowsAppSdkVersion();
        AppVersionText.Text = GetAppVersion();
        ArchitectureText.Text = $"{RuntimeInformation.ProcessArchitecture} ({(Environment.Is64BitProcess ? "64-bit" : "32-bit")} Prozess)";
        
        // Process Info
        using var process = Process.GetCurrentProcess();
        var memoryMb = process.WorkingSet64 / (1024.0 * 1024);
        MemoryUsageText.Text = $"{memoryMb:F1} MB";
        ProcessIdText.Text = process.Id.ToString();

        // Steam Status
        UpdateSteamStatus();
        UpdateSteamApiStatus();

        // Storage
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SAM");
        AppDataPathText.Text = _appDataPath;
        SettingsPathText.Text = Path.Combine(_appDataPath, "settings.json");
        ImageCachePathText.Text = _settingsService.ImageCachePath;
        CacheSizeText.Text = GetDirectorySize(_settingsService.ImageCachePath);

        // Log File
        LoadLogInfo();

        // Migration
        LegacyDataText.Text = _migrationService.HasLegacyData ? Loc.Get("Common.Yes") : Loc.Get("Common.No");
        MigrationStatusText.Text = _migrationService.IsMigrationComplete ? Loc.Get("Common.Yes") : Loc.Get("Common.No");
        MigrateButton.IsEnabled = _migrationService.HasLegacyData && !_migrationService.IsMigrationComplete;
    }

    private void LoadLogInfo()
    {
        try
        {
            // Find the current log file
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SAM");
            
            if (Directory.Exists(logDir))
            {
                var logFiles = Directory.GetFiles(logDir, "sam_winui_*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();
                
                if (logFiles.Count > 0)
                {
                    _currentLogFilePath = logFiles[0];
                    LogFilePathText.Text = Path.GetFileName(_currentLogFilePath);
                    LoadLogContent();
                }
                else
                {
                    LogFilePathText.Text = "Keine Log-Datei gefunden";
                    LogContentText.Text = "Keine Logs verfügbar.";
                }
            }
            else
            {
                LogFilePathText.Text = "Log-Ordner nicht vorhanden";
                LogContentText.Text = "Keine Logs verfügbar.";
            }
        }
        catch (Exception ex)
        {
            LogFilePathText.Text = "Fehler";
            LogContentText.Text = $"Fehler beim Laden der Logs: {ex.Message}";
        }
    }

    private void LoadLogContent()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentLogFilePath) || !File.Exists(_currentLogFilePath))
            {
                LogContentText.Text = "Log-Datei nicht gefunden.";
                return;
            }

            // Read last 50 lines
            var allLines = File.ReadAllLines(_currentLogFilePath);
            var lastLines = allLines.Skip(Math.Max(0, allLines.Length - 50)).ToArray();
            LogContentText.Text = string.Join(Environment.NewLine, lastLines);
        }
        catch (IOException)
        {
            // File might be in use, try with FileShare.ReadWrite
            try
            {
                using var fs = new FileStream(_currentLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var content = sr.ReadToEnd();
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var lastLines = lines.Skip(Math.Max(0, lines.Length - 50)).ToArray();
                LogContentText.Text = string.Join(Environment.NewLine, lastLines);
            }
            catch (Exception ex)
            {
                LogContentText.Text = $"Fehler beim Lesen (Datei in Verwendung): {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            LogContentText.Text = $"Fehler: {ex.Message}";
        }
    }

    private void UpdateSteamApiStatus()
    {
        // Check if steam_api.dll exists in app directory
        var appDir = AppContext.BaseDirectory;
        var steamApiPath = Path.Combine(appDir, "steam_api.dll");
        var steam64ApiPath = Path.Combine(appDir, "steam_api64.dll");
        
        if (File.Exists(steamApiPath))
        {
            SteamApiStatusIcon.Glyph = "\uE73E"; // Checkmark
            SteamApiStatusIcon.Foreground = new SolidColorBrush(Colors.LimeGreen);
            
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(steamApiPath);
                SteamApiStatusText.Text = $"Vorhanden (v{versionInfo.FileVersion ?? "?"})";
            }
            catch
            {
                SteamApiStatusText.Text = "Vorhanden";
            }
        }
        else if (File.Exists(steam64ApiPath))
        {
            SteamApiStatusIcon.Glyph = "\uE73E"; // Checkmark
            SteamApiStatusIcon.Foreground = new SolidColorBrush(Colors.LimeGreen);
            SteamApiStatusText.Text = "steam_api64.dll vorhanden";
        }
        else
        {
            SteamApiStatusIcon.Glyph = "\uE711"; // X
            SteamApiStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
            SteamApiStatusText.Text = "Nicht gefunden";
        }

        // Steam installation path
        SteamPathText.Text = GetSteamInstallPath();
    }

    private static string GetSteamInstallPath()
    {
        try
        {
            // Try to get from registry
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key != null)
            {
                var steamPath = key.GetValue("SteamPath") as string;
                if (!string.IsNullOrEmpty(steamPath) && Directory.Exists(steamPath))
                {
                    return steamPath;
                }
            }

            // Try common paths
            var commonPaths = new[]
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return "Nicht gefunden";
        }
        catch
        {
            return "Fehler beim Ermitteln";
        }
    }

    private async void UpdateSteamStatus()
    {
        // Try to initialize Steam if not already done
        if (!_steamService.IsInitialized)
        {
            try
            {
                // Initialize with Spacewar's AppId (480) - Steam's test app
                _steamService.Initialize(480);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Steam initialization failed: {ex.Message}");
                
                // Show detailed error message
                SteamStatusIcon.Glyph = "\uE711"; // X
                SteamStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                SteamStatusText.Text = $"Fehler: {ex.Message}";
                SteamUserIdText.Text = "-";
                GameCountText.Text = "-";
                return;
            }
        }

        var isConnected = _steamService.IsInitialized;
        
        if (isConnected)
        {
            SteamStatusIcon.Glyph = "\uE73E"; // Checkmark
            SteamStatusIcon.Foreground = new SolidColorBrush(Colors.LimeGreen);
            SteamStatusText.Text = "Verbunden";
            SteamUserIdText.Text = _steamService.SteamId.ToString();
            GameCountText.Text = "Wird geladen...";
            
            // Load game count asynchronously
            try
            {
                var games = await _steamService.GetOwnedGamesAsync();
                var count = games.Count();
                GameCountText.Text = count.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load games: {ex.Message}");
                GameCountText.Text = "Fehler";
            }
        }
        else
        {
            SteamStatusIcon.Glyph = "\uE711"; // X
            SteamStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
            SteamStatusText.Text = "Nicht verbunden (Steam muss gestartet sein)";
            SteamUserIdText.Text = "-";
            GameCountText.Text = "-";
        }
    }

    private static string GetWindowsAppSdkVersion()
    {
        try
        {
            var assembly = typeof(Microsoft.UI.Xaml.Application).Assembly;
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unbekannt";
        }
        catch
        {
            return "Unbekannt";
        }
    }

    private static string GetAppVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "8.0.0";
        }
        catch
        {
            return "8.0.0";
        }
    }

    private static string GetDirectorySize(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return "0 MB";
            }

            var size = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
            
            return size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{size / (1024.0 * 1024):F1} MB",
                _ => $"{size / (1024.0 * 1024 * 1024):F2} GB"
            };
        }
        catch
        {
            return "Fehler beim Berechnen";
        }
    }

    private async void MigrateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MigrateButton.IsEnabled = false;
            MigrationProgress.Visibility = Visibility.Visible;

            var progress = new Progress<int>(p => MigrationProgress.Value = p);
            
            var success = await _migrationService.MigrateImageCacheAsync(progress);
            
            if (success)
            {
                _migrationService.MarkMigrationComplete();
                _notificationService.ShowSuccess("Migration erfolgreich", "Legacy-Daten wurden migriert.");
                LoadDiagnosticInfo();
            }
            else
            {
                _notificationService.ShowError("Migration fehlgeschlagen", "Es gab einen Fehler bei der Migration.");
                MigrateButton.IsEnabled = true;
            }

            MigrationProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "MigrateButton_Click failed");
            _notificationService.ShowError("Migration fehlgeschlagen", ex.Message);
            MigrateButton.IsEnabled = true;
            MigrationProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Cache leeren",
            Content = "Möchtest du den Bild-Cache wirklich leeren?\n\nBilder werden bei Bedarf neu heruntergeladen.",
            PrimaryButtonText = "Leeren",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var cachePath = _settingsService.ImageCachePath;
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, recursive: true);
                    Directory.CreateDirectory(cachePath);
                }
                _notificationService.ShowSuccess("Cache geleert", "Der Bild-Cache wurde erfolgreich geleert.");
                LoadDiagnosticInfo();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Fehler", $"Cache konnte nicht geleert werden: {ex.Message}");
            }
        }
    }

    private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Einstellungen zurücksetzen",
                Content = "Möchtest du alle Einstellungen auf die Standardwerte zurücksetzen?",
                PrimaryButtonText = "Zurücksetzen",
                CloseButtonText = "Abbrechen",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _settingsService.ResetToDefaults();
                await _settingsService.SaveAsync();
                _themeService.SetTheme(ElementTheme.Default);
                _notificationService.ShowInfo("Einstellungen zurückgesetzt", "Die Einstellungen wurden auf Standardwerte zurückgesetzt.");
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "ResetSettingsButton_Click failed");
            _notificationService.ShowError("Fehler", ex.Message);
        }
    }

    private async void OpenAppFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(_appDataPath))
            {
                Directory.CreateDirectory(_appDataPath);
            }
            await Launcher.LaunchFolderPathAsync(_appDataPath);
        }
        catch
        {
            _notificationService.ShowError("Fehler", "Ordner konnte nicht geöffnet werden.");
        }
    }

    private async void OpenInstallFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var installPath = AppContext.BaseDirectory;
            await Launcher.LaunchFolderPathAsync(installPath);
        }
        catch
        {
            _notificationService.ShowError("Fehler", "Installationsordner konnte nicht geöffnet werden.");
        }
    }

    private async void OpenLogFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_currentLogFilePath) && File.Exists(_currentLogFilePath))
            {
                await Launcher.LaunchUriAsync(new Uri($"file:///{_currentLogFilePath.Replace('\\', '/')}"));
            }
            else
            {
                _notificationService.ShowWarning("Keine Log-Datei", "Es wurde keine Log-Datei gefunden.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Fehler", $"Log-Datei konnte nicht geöffnet werden: {ex.Message}");
        }
    }

    private void RefreshLogButton_Click(object sender, RoutedEventArgs e)
    {
        LoadLogInfo();
        _notificationService.ShowInfo("Log aktualisiert", "Die Log-Anzeige wurde aktualisiert.");
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadDiagnosticInfo();
        _notificationService.ShowInfo("Aktualisiert", "Diagnose-Informationen wurden aktualisiert.");
    }

    private void CopyInfoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SAM Diagnose-Informationen ===");
            sb.AppendLine($"Erstellt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            
            sb.AppendLine("--- System ---");
            sb.AppendLine($"Betriebssystem: {OsVersionText.Text}");
            sb.AppendLine($".NET Runtime: {DotNetVersionText.Text}");
            sb.AppendLine($"Windows App SDK: {WinAppSdkVersionText.Text}");
            sb.AppendLine($"App Version: {AppVersionText.Text}");
            sb.AppendLine($"Prozess-Architektur: {ArchitectureText.Text}");
            sb.AppendLine($"Speicherverbrauch: {MemoryUsageText.Text}");
            sb.AppendLine($"Prozess-ID: {ProcessIdText.Text}");
            sb.AppendLine();
            
            sb.AppendLine("--- Steam ---");
            sb.AppendLine($"Verbindungsstatus: {SteamStatusText.Text}");
            sb.AppendLine($"Steam User ID: {SteamUserIdText.Text}");
            sb.AppendLine($"Anzahl Spiele: {GameCountText.Text}");
            sb.AppendLine($"Steam-Installationspfad: {SteamPathText.Text}");
            sb.AppendLine($"steam_api.dll Status: {SteamApiStatusText.Text}");
            sb.AppendLine();
            
            sb.AppendLine("--- Speicher ---");
            sb.AppendLine($"App-Datenordner: {AppDataPathText.Text}");
            sb.AppendLine($"Einstellungsdatei: {SettingsPathText.Text}");
            sb.AppendLine($"Bild-Cache: {ImageCachePathText.Text}");
            sb.AppendLine($"Cache-Größe: {CacheSizeText.Text}");
            sb.AppendLine();
            
            sb.AppendLine("--- Migration ---");
            sb.AppendLine($"Legacy-Daten vorhanden: {LegacyDataText.Text}");
            sb.AppendLine($"Migration abgeschlossen: {MigrationStatusText.Text}");
            sb.AppendLine();
            
            sb.AppendLine("--- Log-Datei ---");
            sb.AppendLine($"Aktuelle Log-Datei: {LogFilePathText.Text}");

            var dataPackage = new DataPackage();
            dataPackage.SetText(sb.ToString());
            Clipboard.SetContent(dataPackage);
            
            _notificationService.ShowSuccess("Kopiert", "Diagnose-Informationen wurden in die Zwischenablage kopiert.");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Fehler", $"Kopieren fehlgeschlagen: {ex.Message}");
        }
    }

    private void TestToastButton_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.ShowInfo("Test-Benachrichtigung", "Dies ist eine Test-Nachricht vom SAM WinUI!");
    }

    private void TestThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var currentTheme = _themeService.CurrentTheme;
        var newTheme = currentTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Default,
            _ => ElementTheme.Light
        };
        
        _themeService.SetTheme(newTheme);
        
        var themeName = newTheme switch
        {
            ElementTheme.Light => "Hell",
            ElementTheme.Dark => "Dunkel",
            _ => "System"
        };
        
        _notificationService.ShowInfo("Theme gewechselt", $"Neues Theme: {themeName}");
    }

    private async void TestSteamButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _notificationService.ShowInfo("Steam-Test", "Versuche Steam-Verbindung...");
            
            // Force re-initialization
            if (_steamService.IsInitialized)
            {
                // Already connected
                var games = await _steamService.GetOwnedGamesAsync();
                var count = games.Count();
                _notificationService.ShowSuccess("Steam verbunden", $"Erfolgreich! {count} Spiele gefunden.");
            }
            else
            {
                try
                {
                    _steamService.Initialize(480); // Spacewar test app
                    _notificationService.ShowSuccess("Steam verbunden", $"Verbunden als User {_steamService.SteamId}");
                    UpdateSteamStatus();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError("Steam-Fehler", $"Verbindung fehlgeschlagen: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Steam-Fehler", $"Test fehlgeschlagen: {ex.Message}");
        }
    }
}
