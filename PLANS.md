# Roadmap – Steam Achievement Manager (SAM-Plus)

Übersicht aller geplanten, laufenden und abgeschlossenen Features.  
Legende: ✅ Abgeschlossen | 🔴 Hohe Priorität | 🟡 Mittlere Priorität | 🟢 Niedrige Priorität

---

## � Sicherheit & VAC – Wichtige Information

> **Wird SAM von Steam/VAC erkannt?**
> 
> **Kurze Antwort: Nein.** SAM ist seit über 15 Jahren in Benutzung und Valve hat nie Maßnahmen dagegen ergriffen.
> 
> **Warum?**
> - SAM nutzt die **offizielle Steam API** (`steamclient.dll`) – keine Hacks, Injections oder Modifikationen
> - Achievements sind **NICHT VAC-geschützt** – VAC überwacht nur Multiplayer-Cheats
> - Die API-Aufrufe (`SetAchievement`, `StoreStats`) sind **legitime Steamworks-Funktionen**
> - SAM **injiziert keinen Code** in Spiele und modifiziert keine Dateien
> 
> **Risiken:**
> - Einige Spiele mit **serverseitiger Achievement-Validierung** (z.B. MMOs) können Achievements zurücksetzen
> - **Leaderboard-Checks** können verhindern, dass Achievements zählen
> - Dein **Steam-Profil zeigt den Unlock-Zeitpunkt** – unrealistische Zeiten sind sichtbar
> 
> **Empfehlung:** Für maximale Diskretion, nutze die geplanten Privacy-Features (s. unten).

---

## 🔴 Hohe Priorität (Stabilität & Modernisierung)

### Stabilität & Fehlerbehandlung
- [x] **Globale Fehlerbehandlung** - Globales Exception Handling (`Application.ThreadException`, `UnhandledException`) implementieren, um Abstürze ohne Log zu verhindern
- [x] **Ressourcen-Cleanup (IDisposable)** - Korrektes Disposing von Netzwerk-Clients (`WebClient`, `IconDownloader`) beim Schließen von Fenstern
- [x] **Logging-System** - Einführung eines Logging-Frameworks (z.B. Serilog oder NLog) für Datei-Logs bei Fehlern

### Technische Modernisierung
- [x] **Migration auf .NET 8** - Upgrade des Projekts von .NET Framework 4.8 auf .NET 8 für Performance und neue C#-Features
- [x] **Netzwerk-Layer Erneuerung** - Veralteten `WebClient` durch `HttpClient` ersetzen (Problemvermeidung bei Timeouts/Proxies)
- [x] **Async/Await Pattern** - Refactoring von blockierenden Calls und `BackgroundWorker` hin zu modernem `async`/`await` Task-Pattern

---

## 🟡 Mittlere Priorität (Architektur & Wartbarkeit)

### Code-Struktur
- [x] **Konfiguration auslagern** - Hardcodierte URLs (z.B. `gib.me/sam/games.xml`) in eine `appsettings.json` oder `App.config` extrahieren
- [x] **Typsicherheit im Binding** - Ersetzen von "Magic Strings" im DataBinding (z.B. `"DisplayName"`) durch `nameof()`-Operatoren
- [x] **Dependency Injection (DI)** - Einführung eines simplen DI-Containers für Services (z.B. `SteamClient`, `LogService`), statt Weitergabe per Konstruktor

### Qualitätssicherung & Build
- [ ] **Unit Tests** - Erstellung einer Test-Suite (xUnit/NUnit) für die `SAM.API` Wrapper und Logik-Klassen (ohne UI)
- [ ] **GitHub Actions CI** - Ersetzen des alten AppVeyor-Builds durch moderne GitHub Actions Workflows (Build & Release)
- [ ] **Code-Analyse** - Aktivierung von strengeren Roslyn-Analyzers (NetAnalyzers) zur Code-Qualitätssicherung

### Feature-Erweiterungen
- [ ] **Sicherheits-Abfragen** - Bestätigungs-Dialoge ("Hold to Confirm") für kritische Aktionen wie "Unlock All" oder "Reset All"
- [ ] **Statistik-Backup** - Automatisches lokales Backup der `UserStats` als JSON vor jeder Änderung
- [ ] **Achievement-Import/Export** - Export der Achievement-Daten als JSON/XML zum Übertragen zwischen Accounts
- [ ] **Batch-Operationen** - Mehrere Spiele gleichzeitig auswählen und Achievements bearbeiten
- [ ] **Achievement-Vorlagen** - Speichern von Achievement-Sets als Vorlagen für schnelles Anwenden

### 🔒 Privatsphäre & Diskretion
- [ ] **Realistische Unlock-Zeiten** - Zufällige Verzögerung zwischen Achievement-Unlocks (5-30 Min) um natürliches Spielverhalten zu simulieren
- [ ] **Spielzeit-Check** - Warnung wenn Spielzeit < erwartete Zeit für Achievement (z.B. "100 Stunden gespielt" nach 2h)
- [ ] **Zeitstempel-Anpassung** - Manuelles Setzen von Unlock-Zeitpunkten (falls Steam API es erlaubt)
- [ ] **Profil-Vorschau** - Anzeige wie das Steam-Profil nach Änderungen aussehen wird
- [ ] **"Safe Mode"** - Modus der nur Achievements freigibt die normalerweise erreichbar wären (basierend auf Spielzeit/Stats)
- [ ] **Lokale Historien-Löschung** - Option zum Löschen aller lokalen Logs und Achievement-Cache

---

## 🟢 Niedrige Priorität (UX & Optimierung)

### UI / UX Verbesserungen
- [x] **High DPI Awareness** - Unterstützung für Per-Monitor DPI Skalierung (scharfe Schrift auf 4K-Monitoren)
- [x] **Dark Mode / Theming** - Implementierung eines dunklen Farbschemas für `GamePicker` und `Manager`
- [x] **Erweiterte Suche/Filter** - Im `Manager`: Filtern nach "Gesperrt", "Freigeschaltet" oder versteckten Achievements
- [ ] **Bessere Ladeindikatoren** - Moderne Spinner/Skeleton-Loading statt blockierender UI beim Laden von Icons
- [ ] **Spiel-Favoriten** - Häufig genutzte Spiele als Favoriten markieren (schneller Zugriff)
- [ ] **Sortier-Optionen** - Achievement-Liste nach Name, Seltenheit, Unlock-Datum sortieren
- [ ] **Achievement-Statistik** - Zeige Gesamtfortschritt, seltenste Achievements, Completion-Rate
- [ ] **Mehrsprachigkeit (i18n)** - Übersetzungen für DE, EN, FR, ES über Resource-Dateien
- [ ] **Tastenkürzel** - Shortcuts für häufige Aktionen (Ctrl+S = Store, Ctrl+R = Refresh)

### Performance & Optimierung
- [x] **Icon-Caching** - Lokaler Cache für heruntergeladene Achievement-Icons (`%LOCALAPPDATA%\SAM-Plus\IconCache`)
- [x] **Lazy Loading** - Achievement-Icons erst laden wenn sie sichtbar werden (nur sichtbare Queue-Items)
- [x] **Parallel Downloads** - Mehrere Icons gleichzeitig herunterladen mit `SemaphoreSlim` (max 5 parallel)
- [ ] **Startup-Zeit** - Verzögertes Laden von nicht-kritischen Komponenten
- [x] **Memory-Optimierung** - Garbage Collection nach großen Icon-Downloads (>100MB Threshold)

### Code-Kosmetik
- [ ] **EditorConfig** - Standardisierung von Formatierung (Tabs/Spaces, Braces) über `.editorconfig`
- [ ] **File-Scoped Namespaces** - Umstellung auf die kürzere Namespace-Syntax (C# 10+)
- [ ] **Symbol-Package** - Erstellung von NuGet-Symbolpaketen (`.snupkg`) für besseres Debugging
- [ ] **Source Generators** - Boilerplate-Code (INotifyPropertyChanged) via Roslyn Source Generators
- [ ] **Records statt Classes** - Immutable DTOs als `record` Types für bessere Lesbarkeit

---

## 🚀 Zukünftige Feature-Ideen

| Feature | Komplexität | Beschreibung |
|---------|-------------|--------------|
| **Steam Deck Support** | 🟡 Mittel | Linux-Build testen, Controller-Navigation |
| **Achievement Showcase** | 🟢 Niedrig | Generiere Bilder für Social Media ("100% Complete!") |
| **Steam Web API Integration** | 🟡 Mittel | Zeige Global-Stats, Seltenheits-Prozente |
| **Cloud Sync** | 🔴 Hoch | Lokale Einstellungen über mehrere PCs synchronisieren |
| **Plugin-System** | 🔴 Hoch | Erweiterbarkeit durch Community-Plugins |
| **CLI-Modus** | 🟡 Mittel | Kommandozeilen-Interface für Scripting/Automation |

---

## 🎨 UI Modernisierung – Avalonia UI Migration

> **Ziel:** Migration von Windows Forms zu Avalonia UI für eine moderne, schnelle und Cross-Platform fähige Benutzeroberfläche.

### Warum Avalonia UI?

| Aspekt | WinForms (aktuell) | Avalonia UI (Ziel) |
|--------|--------------------|--------------------|
| **Look & Feel** | Windows 95 Ästhetik | Fluent Design, Acrylic, Shadows |
| **Cross-Platform** | ❌ Nur Windows | ✅ Windows, Linux, macOS |
| **Steam Deck** | ❌ Nicht möglich | ✅ Nativer Linux-Support |
| **Dark Mode** | 🟡 Manuell implementiert | ✅ Built-in Themes |
| **Animationen** | ❌ Keine | ✅ Smooth Transitions |
| **MVVM** | 🟡 Schwierig | ✅ Natives Pattern |
| **Hot Reload** | ❌ Nein | ✅ Ja |

### Phase 1: Vorbereitung (🟢 Niedrig)

- [ ] **Avalonia Template installieren** - `dotnet new install Avalonia.Templates`
- [ ] **Neues Projekt erstellen** - `SAM.UI` als Avalonia Desktop Projekt
- [ ] **Shared Library** - `SAM.Core` erstellen für gemeinsame Logik (ViewModels, Services)
- [ ] **NuGet Pakete** - Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, CommunityToolkit.Mvvm

### Phase 2: MVVM Architektur (🟡 Mittel)

- [ ] **ViewModels erstellen**
  - `GamePickerViewModel` - Spiel-Auswahl Logik
  - `ManagerViewModel` - Achievement-Verwaltung
  - `SettingsViewModel` - Einstellungen
- [ ] **Services abstrahieren**
  - `ISteamService` - Steam API Wrapper
  - `IIconCacheService` - Icon-Caching
  - `IConfigService` - Konfiguration
- [ ] **CommunityToolkit.Mvvm** - `[ObservableProperty]`, `[RelayCommand]` Attribute

### Phase 3: Views migrieren (🔴 Hoch)

- [ ] **MainWindow** - Haupt-Container mit Navigation
- [ ] **GamePickerView** - Spiel-Liste mit virtualisierten Icons
- [ ] **ManagerView** - Achievement-Liste mit Tabs
  - Achievement-Tab mit Checkboxen
  - Statistik-Tab mit DataGrid
- [ ] **SettingsView** - Theme, Cache, Privacy-Optionen

### Phase 4: UI/UX Design (🟡 Mittel)

- [ ] **Fluent Theme** - `<FluentTheme Mode="Dark"/>` oder `Light`
- [ ] **Custom Styles** - Eigene Farben, Fonts, Spacing
- [ ] **Icons** - Fluent System Icons oder Material Design Icons
- [ ] **Animationen** - Fade-In für Listen, Slide für Navigation
- [ ] **Responsive Layout** - Grid/StackPanel für verschiedene Fenstergrößen

### Phase 5: Erweiterte Features (🟢 Niedrig)

- [ ] **System Tray** - Minimieren in Tray mit Benachrichtigungen
- [ ] **Acrylic/Mica** - Transparenz-Effekte (Windows 11)
- [ ] **Drag & Drop** - Achievement-Reihenfolge ändern
- [ ] **Context Menus** - Rechtsklick-Aktionen
- [ ] **Keyboard Navigation** - Vollständige Tastatursteuerung

### Projektstruktur (Ziel)

```
SAM-Plus/
├── SAM.API/              # Bestehend - Steam API Wrapper
├── SAM.Core/             # NEU - Shared Logic
│   ├── ViewModels/
│   ├── Services/
│   └── Models/
├── SAM.UI/               # NEU - Avalonia UI
│   ├── Views/
│   ├── Controls/
│   ├── Styles/
│   └── App.axaml
├── SAM.Game/             # DEPRECATED - WinForms (Übergang)
└── SAM.Picker/           # DEPRECATED - WinForms (Übergang)
```

### Migrations-Timeline

| Phase | Dauer | Priorität |
|-------|-------|-----------|
| Phase 1: Setup | 1-2 Tage | 🟢 Start |
| Phase 2: MVVM | 3-5 Tage | 🟡 |
| Phase 3: Views | 5-7 Tage | 🔴 Hauptarbeit |
| Phase 4: Polish | 2-3 Tage | 🟡 |
| Phase 5: Extras | Ongoing | 🟢 |

### Ressourcen

- [Avalonia Dokumentation](https://docs.avaloniaui.net/)
- [Avalonia Templates](https://github.com/AvaloniaUI/avalonia-dotnet-templates)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Fluent Avalonia](https://github.com/amwx/FluentAvalonia) - Windows 11 Style

---

## 📋 Bekannte Probleme

| Problem | Schwere | Beschreibung |
|---------|---------|--------------|
| ~~Absturz ohne Log~~ | ✅ Behoben | Globales Exception Handling + Logger implementiert |
| ~~WebClient Obsolete~~ | ✅ Behoben | Ersetzt durch `HttpClient` |
| ~~Hardcodierte URLs~~ | ✅ Behoben | URLs in appsettings.json ausgelagert |
| ~~UI Freezes~~ | ✅ Behoben | Async/Await implementiert |
| ~~Binding Strings~~ | ✅ Behoben | Ersetzt durch nameof() |
| Serverseitige Validierung | ⚠️ Limitation | Einige Spiele (MMOs) prüfen Achievements serverseitig |

---

## 📊 Projekt-Status

| Bereich | Technologie | Details |
|---------|------------|---------|
| **Framework** | .NET 8.0 | ✅ Migriert von .NET Framework 4.8 |
| **UI** | Windows Forms → Avalonia | 🔄 Migration zu Avalonia UI geplant |
| **API Wrapper** | P/Invoke / Interop | Direkter Wrapper um `steam_api.dll` Interfaces |
| **Netzwerk** | HttpClient | ✅ Ersetzt veralteten WebClient |
| **Konfiguration** | appsettings.json | ✅ URLs ausgelagert via AppConfig |
| **DI** | ServiceLocator | ✅ Einfacher Service-Container |
| **Build** | MSBuild / dotnet CLI | Migration zu GitHub Actions geplant |
| **Architektur** | Code-Behind → MVVM | 🔄 Migration zu MVVM mit Avalonia |
