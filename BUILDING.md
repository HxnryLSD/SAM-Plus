# Build-Anleitung / Build Instructions

## Quick Start

### Empfohlen: Komplettes Projekt bauen

```powershell
# Beide Apps bauen (gemeinsamer Ausgabeordner)
# Verwende Visual Studio's MSBuild (NICHT dotnet build!)
& "$(& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe)" SAM.sln -p:Platform=x86 -p:Configuration=Release -verbosity:minimal

# Oder wenn msbuild im PATH ist:
msbuild SAM.sln -p:Platform=x86 -p:Configuration=Release -verbosity:minimal
```

> **Wichtig:** `dotnet build` funktioniert NICHT für WinUI-Projekte!
> Das Windows App SDK benötigt Visual Studio's MSBuild für die PRI-Dateigenerierung.

Die ausführbaren Dateien befinden sich in:
- `bin\x86\Release\SAM.WinUI.exe` (Game Picker)
- `bin\x86\Release\SAM.Manager.exe` (Achievement Manager)

### Einzelne Projekte bauen

```powershell
# SAM.WinUI (Game Picker)
msbuild SAM.WinUI\SAM.WinUI.csproj -p:Platform=x86 -p:Configuration=Release

# SAM.Manager (Achievement Manager)
msbuild SAM.Manager\SAM.Manager.csproj -p:Platform=x86 -p:Configuration=Release
```

---

## Voraussetzungen / Prerequisites

### Für SAM.WinUI + SAM.Manager (Empfohlen):
- **Visual Studio 2022/2026** (Version 17.8 oder höher) mit:
  - ".NET Desktop Development" Workload
  - "Windows application development" Workload (enthält Windows App SDK)
  - Windows 10 SDK (19041 oder höher)

**Hinweis:** SAM.WinUI/SAM.Manager können **nicht** mit `dotnet build` gebaut werden!
Das Windows App SDK benötigt Visual Studio's MSBuild für die PRI (Package Resource Index)
Dateigenerierung. Der .NET SDK CLI (`dotnet build`) unterstützt diese Build-Tasks nicht.

### Für SAM.API und SAM.Core:
Diese werden automatisch als Dependencies von SAM.WinUI/SAM.Manager gebaut.
Einzeln per `dotnet build` möglich (nur x86):
```powershell
dotnet build SAM.API\SAM.API.csproj -c Release
dotnet build SAM.Core\SAM.Core.csproj -c Release
```

---

## Build-Befehle

> **Wichtig:** Alle Befehle müssen im Projektverzeichnis ausgeführt werden (dort wo `SAM.sln` liegt).

### SAM.WinUI + SAM.Manager (WinUI 3 - Empfohlen)

**In Visual Studio:**
1. Öffne `SAM.sln` in Visual Studio 2022/2026
2. Wähle `SAM.WinUI` als Startprojekt
3. Wähle Plattform: `x86` (empfohlen), `x64` oder `arm64`
4. Build Solution (Ctrl+Shift+B)

**Kommandozeile (mit Visual Studio installiert):**
```powershell
# Debug Build (komplette Solution)
msbuild SAM.sln -p:Platform=x86 -p:Configuration=Debug -verbosity:minimal

# Release Build (komplette Solution)
msbuild SAM.sln -p:Platform=x86 -p:Configuration=Release -verbosity:minimal

# Einzelne Projekte
msbuild SAM.WinUI\SAM.WinUI.csproj -p:Platform=x86 -p:Configuration=Debug
msbuild SAM.Manager\SAM.Manager.csproj -p:Platform=x86 -p:Configuration=Debug
```

### Legacy-Projekte (Deprecated - NICHT in Solution)

Die Legacy WinForms-Projekte befinden sich im `Deprecated/` Ordner und sind **nicht** Teil von SAM.sln.

```powershell
# Falls benötigt (nicht empfohlen):
dotnet build Deprecated\SAM.Game\SAM.Game.csproj -c Debug -p:Platform=x86
dotnet build Deprecated\SAM.Picker\SAM.Picker.csproj -c Debug -p:Platform=x86
```

> ⚠️ **Hinweis:** SAM.Game und SAM.Picker sind veraltet und werden nicht mehr gepflegt.
> Bitte verwende SAM.WinUI + SAM.Manager.

---

## Projektstruktur

| Projekt | Beschreibung | Framework | Plattformen | Status |
|---------|--------------|-----------|-------------|--------|
| **SAM.WinUI** | Game Picker (WinUI 3) | net10.0-windows10.0.19041.0 | x86, x64, arm64 | ✅ Aktiv |
| **SAM.Manager** | Achievement Manager (WinUI 3) | net10.0-windows10.0.19041.0 | x86, x64, arm64 | ✅ Aktiv |
| **SAM.Core** | Shared ViewModels & Services | net10.0-windows10.0.19041.0 | x86 | ✅ Aktiv |
| **SAM.Core.Tests** | Unit Tests für SAM.Core | net10.0-windows10.0.19041.0 | x86 | ✅ Aktiv |
| **SAM.API** | Steam API Wrapper | net10.0-windows | x86 | ✅ Aktiv |

<details>
<summary>Legacy-Projekte (Deprecated/ Ordner, nicht in SAM.sln)</summary>

| Projekt | Beschreibung | Framework | Status |
|---------|--------------|-----------|--------|
| SAM.Game | Achievement Manager (WinForms) | net10.0-windows | ❌ Deprecated |
| SAM.Picker | Game Picker (WinForms) | net10.0-windows | ❌ Deprecated |

</details>

### Architektur

```
SAM.WinUI (Game Picker)          SAM.Manager (Achievement Manager)
         │                                │
         └──────────┬─────────────────────┘
                    │
                SAM.Core (ViewModels, Services, Logging)
                    │
                SAM.API (Steam API Wrapper)
```

**Warum zwei Apps?**
Steam bindet die AppID beim Prozessstart. SAM.WinUI startet mit AppID=0 (kein Spiel), 
dann wird SAM.Manager mit der spezifischen Game-AppID gestartet.

---

## Unit Tests

Das Projekt enthält Unit Tests für SAM.Core in `SAM.Core.Tests/`.

### Tests ausführen

```powershell
# Alle Tests in der Solution ausführen
dotnet test

# Alle Tests ausführen (nur SAM.Core.Tests)
dotnet test SAM.Core.Tests\SAM.Core.Tests.csproj

# Mit detaillierter Ausgabe
dotnet test SAM.Core.Tests\SAM.Core.Tests.csproj --logger "console;verbosity=detailed"

# Mit Code Coverage (Ergebnis in TestResults/)
dotnet test SAM.Core.Tests\SAM.Core.Tests.csproj --collect:"XPlat Code Coverage"
```

### Test-Struktur

| Ordner | Beschreibung |
|--------|--------------|
| `Mocks/` | Mock-Implementierungen für Services (ISteamService, IAchievementService, etc.) |
| `ViewModels/` | Tests für GamePickerViewModel, AchievementManagerViewModel |
| `Services/` | Tests für AchievementService, SteamCallbackService, DrmProtectionService, GameCacheService, LocalizationService, etc. |
| `Utilities/` | Tests für AppPaths, SteamErrorHelper |

### Neue Test-Schwerpunkte

- Steam-Fehlertexte: SteamErrorHelper (Pattern-Matches, Fallbacks)
- Lokalisierung: LocalizationService (Fallbacks, Events)
- Steam-Callbacks: TranslateResultCode + Retry-Logik
- DRM-Protection: Schema-Parsing + Caching
- SQLite Cache: GameCacheService (In-Memory)
- ViewModel-Basics: ViewModelBase (Busy-Guard, Errors, Cancellation)
- App-Pfade: AppPaths (User/Game/Logs, Sanitizing)

### Test-Framework

| Package | Version | Zweck |
|---------|---------|-------|
| xunit | 2.9.2 | Test-Framework |
| Moq | 4.20.72 | Mocking-Framework |
| coverlet.collector | 6.0.2 | Code Coverage |

> **Hinweis:** Die Tests benötigen **keinen** Steam-Client, da alle Steam-Abhängigkeiten 
> durch Mock-Services ersetzt werden. Die Tests können auf jedem System ausgeführt werden.

---

## Ausgabeordner

Alle Projekte verwenden einen gemeinsamen Ausgabeordner (konfiguriert in `Directory.Build.props`):

| Konfiguration | Pfad |
|---------------|------|
| Debug x86 | `bin\x86\Debug\` |
| Release x86 | `bin\x86\Release\` |
| Debug x64 | `bin\x64\Debug\` |
| Release x64 | `bin\x64\Release\` |
| Debug arm64 | `bin\arm64\Debug\` |
| Release arm64 | `bin\arm64\Release\` |

---

## Plattformen

| Plattform | SAM.API | SAM.Core | SAM.WinUI | SAM.Manager |
|-----------|---------|----------|-----------|-------------|
| **x86** (32-bit) | ✅ | ✅ | ✅ | ✅ |
| **x64** (64-bit) | ❌ | ❌ | ✅ | ✅ |
| **arm64** | ❌ | ❌ | ✅ | ✅ |

> **Hinweis:** Nur **x86** wird für Produktions-Builds empfohlen, da SAM.API und SAM.Core 
> nur x86 unterstützen. x64/arm64 Builds von SAM.WinUI/SAM.Manager funktionieren nur,
> wenn die x86-Dependencies verfügbar sind (AnyCPU-Fallback).

---

## Bekannte Probleme

### Debug-Build startet nicht (XamlParseException)

**Symptom:** Debug-Build kompiliert erfolgreich, aber die App crasht beim Start mit:
```
Microsoft.UI.Xaml.Markup.XamlParseException: XAML parsing failed.
   at SAM.WinUI.MainWindow.InitializeComponent()
```

**Ursache:** WinUI generiert unterschiedliche PRI/XBF-Formate für Debug vs Release. 
Bei gemeinsamen Ausgabeverzeichnissen (wie in diesem Projekt) entsteht eine Inkompatibilität.

**Lösung:** Zuerst Release bauen, dann Debug:
```powershell
# 1. Erst Release bauen
msbuild SAM.sln -p:Platform=x86 -p:Configuration=Release -verbosity:minimal

# 2. Dann Debug bauen (kopiert automatisch Release-PRI/XBF)
msbuild SAM.sln -p:Platform=x86 -p:Configuration=Debug -verbosity:minimal
```

Der Debug-Build erkennt automatisch vorhandene Release-Assets und kopiert diese.
Die Meldung `Copied Release PRI/XBF to Debug output (WinUI shared output workaround)` 
bestätigt den erfolgreichen Workaround.

### SAM.Manager startet nicht (fehlende PRI-Datei)

**Symptom:** SAM.Manager kompiliert erfolgreich, crasht aber beim Start ohne Fehlermeldung.
Das Log endet nach dem Konstruktor ohne XAML-Initialisierung.

**Ursache:** Bei WinUI-Apps mit `WindowsPackageType=None` (unpackaged) generiert MSBuild 
standardmäßig `resources.pri`. Für das korrekte Laden der XAML-Ressourcen muss diese 
Datei aber `{ProjectName}.pri` heißen (z.B. `SAM.Manager.pri`).

**Lösung:** Die Projektdateien enthalten einen `RenameResourcesPri` Build-Target, der 
`resources.pri` automatisch in `SAM.Manager.pri` bzw. `SAM.WinUI.pri` umbenennt.
Bei Problemen: Rebuild des betroffenen Projekts ausführen:
```powershell
msbuild SAM.Manager\SAM.Manager.csproj -p:Platform=x86 -p:Configuration=Release -t:Rebuild
```

### MrtCore.PriGen Fehler beim Bauen ohne Visual Studio
```
error MSB4062: The "Microsoft.Build.Packaging.Pri.Tasks.ExpandPriContent" task could not be loaded
```

**Lösung:** Visual Studio 2022/2026 mit den oben genannten Workloads installieren.

### Windows App SDK Version
Das Projekt verwendet Windows App SDK 1.6.x. Alle Package-Versionen sind in 
`Directory.Build.props` zentralisiert. Bei Problemen:
```powershell
dotnet restore SAM.sln
```

---

## Release erstellen

1. **Build in Release-Konfiguration:**
   ```powershell
   msbuild SAM.sln -p:Platform=x86 -p:Configuration=Release -verbosity:minimal
   ```

2. **Ausgabe-Ordner:**
   ```
   bin\x86\Release\
   ├── SAM.WinUI.exe      (Game Picker - Hauptanwendung)
   ├── SAM.Manager.exe    (Achievement Manager)
   ├── SAM.Core.dll
   ├── SAM.API.dll
   ├── Assets\
   └── [alle weiteren DLLs]
   ```

3. **Für Distribution:**
   Der gesamte Inhalt von `bin\x86\Release\` wird verteilt.
   
> **Hinweis:** SAM.WinUI.exe ist die Hauptanwendung. Sie startet SAM.Manager.exe 
> automatisch für jedes ausgewählte Spiel.
