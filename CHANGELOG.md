# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Settings sync between SAM.WinUI and SAM.Manager using shared SQLite storage.
- Startup update check against GitHub Releases with a Settings toggle.
- Accent color selection (system or custom) in Settings.

### Fixed
- Restrict TLS certificate exceptions to `gib.me` instead of globally accepting all certificates.
- Implemented StatisticsPage save flow to persist modified stats via Steam and report errors.

### Changed
- Removed concrete cast from `AchievementService` by exposing `SetClient` on `ISteamCallbackService`.

---

## [8.0.0] - 2026-02-10

### Added
- **SAM.WinUI** - Complete UI rewrite using WinUI 3 and Windows App SDK
  - Modern Fluent Design with Mica backdrop
  - Automatic Dark/Light theme support
  - NavigationView-based layout
  - GridView game picker with `GameCard` controls
  - `AchievementCard` control for achievement display
  - Settings page with theme selection
  - Diagnostics page for troubleshooting
  - About page with project information
  - Toast notifications for save operations

- **SAM.Manager** - Dedicated WinUI 3 achievement manager for individual games
  - `AchievementManagerPage` with progress tracking
  - `StatisticsPage` for editing game statistics
  - `ConfettiControl` for celebration effects
  - `NotificationBar` control for in-app notifications

- **SAM.Core** - New shared library with MVVM architecture
  - ViewModels: `GamePickerViewModel`, `AchievementManagerViewModel`, `ViewModelBase` using CommunityToolkit.Mvvm
  - Service layer with dependency injection:
    - `ISteamService` - Steam client wrapper
    - `IAchievementService` - Achievement management
    - `IImageCacheService` - Icon caching with HttpClient
    - `ISettingsService` - Persistent settings storage (SQLite via Microsoft.Data.Sqlite)
    - `ILegacyMigrationService` - Migration from legacy versions
    - `IDrmProtectionService` - DRM protection detection for games
    - `ILibraryFetchService` - Steam library fetching and caching
    - `ILocalizationService` - Localization support
    - `ISteamCallbackService` - Steam callback handling
    - `IUserDataService` - User data persistence
    - `IGameCacheService` - Game data caching
  - Models: `GameModel`, `AchievementModel`, `StatModel`, `GameUserData`
  - Utilities: `AppPaths`, `KeyValue`, `KeyValueType`, `SteamErrorHelper`, `StreamHelpers`

- **SAM.Core.Tests** - Unit test suite using xUnit and Moq
  - Service tests: `AchievementServiceTests`, `ImageCacheServiceTests`, `SettingsServiceTests`, `UserDataServiceTests`
  - ViewModel tests: `AchievementManagerViewModelTests`, `GamePickerViewModelTests`

- **Multi-Platform Support**
  - x86 (32-bit) for SAM.API and SAM.Core (Steam API compatibility)
  - x86, x64, arm64 for SAM.WinUI and SAM.Manager

### Changed
- Upgraded all projects to .NET 10.0
- Replaced `WebClient` with `HttpClient` for all network operations
- Replaced `BackgroundWorker` with async/await patterns
- Enabled Nullable Reference Types across all projects
- Updated NuGet packages to latest versions

### Deprecated
- **SAM.Picker** - Legacy WinForms game picker (use SAM.WinUI instead)
- **SAM.Game** - Legacy WinForms achievement manager (use SAM.Manager instead)

### Fixed
- Various null reference warnings
- Thread safety improvements in async operations

### Removed
- Fugue Icons set (replaced with custom app icons)
- Unused NuGet packages: CommunityToolkit.WinUI.Controls.Primitives, Sizers, Animations
- SAM.UI.Shared project (consolidated into SAM.Core)
- Duplicate LoggingService implementations (consolidated into SAM.Core)

---

## [7.0.x] - 2019-2024

### Added
- Open-source release
- GitHub repository
- AppVeyor CI integration

### Changed
- General code maintenance and modernization
- Icons replaced with Fugue Icons set
- Version bumped to 7.0.x.x for open-source release

---

## [6.x and earlier] - 2008-2013

### Notes
- Closed-source releases
- Original release in 2008
- Last major release in 2011
- Final hotfix in 2013

---

## Migration Guide

### From v7.x to v8.0

1. **Download SAM.WinUI** from the releases page
2. **Run SAM.WinUI.exe** - no installation required
3. **Migrate cached images** (optional):
   - Open Settings ? Diagnostics
   - Click "Legacy-Daten migrieren"
4. **Enjoy the new UI!**

### System Requirements for v8.0

- Windows 10 version 1809 (Build 17763) or later
- Windows 11 recommended
- .NET 10.0 Runtime
- Steam Client running and logged in
