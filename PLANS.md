# Steam Achievement Manager - Roadmap

## 📋 Priorisierung

| Priorität | Task | Aufwand | Status |
|-----------|------|---------|--------|
| 🔴 **P0** | SSL-Validierung fixen (Sicherheitslücke) | Niedrig | Erledigt |
| 🔴 **P0** | StatisticsPage Save-Button implementieren | Mittel | Erledigt |
| 🔴 **P1** | Concrete Cast / Interface-Leaking beheben | Niedrig | Offen |
| 🔴 **P1** | Code-Duplikation eliminieren (Converters, Schema) | Mittel | Offen |
| 🔴 **P1** | Hardcoded Strings → Lokalisierung | Mittel | Offen |
| 🟡 **P1** | Tests: Tier 1 — Pure Logic (SteamErrorHelper, Loc, VM) | Niedrig | Offen |
| 🟡 **P2** | God Classes aufbrechen (DiagnosticsPage, SettingsPage) | Hoch | Offen |
| 🟡 **P2** | Code-Behind ausdünnen → ViewModels | Hoch | Offen |
| 🟡 **P2** | async void / ungenutzte Services bereinigen | Niedrig | Offen |
| 🟡 **P2** | Tests: Tier 2 — DRM, GameCache, ViewModelBase, AppPaths | Mittel | Erledigt |
| 🟢 **P3** | ConfigureAwait, Magic Numbers, Logging | Mittel | Offen |
| 🟢 **P3** | Tests: Tier 3 — Integration, Error-Paths, DI-Wiring | Hoch | Offen |
| 🔴 **Hoch** | `Deprecated/` Ordner löschen | Niedrig | Offen |
| 🟡 **Mittel** | Sortier- & Filteroptionen im Game Picker | Mittel | Offen |
| 🟡 **Mittel** | Accessibility (Screenreader, Hochkontrast) | Mittel | Offen |
| 🟡 **Mittel** | Export/Import für Achievements | Mittel | Offen |
| 🟡 **Mittel** | Weitere Sprachen (Lokalisierung) | Mittel | Offen |
| 🟡 **Mittel** | Performance: Startup & Lazy Loading | Mittel | Offen |
| 🟢 **Niedrig** | Achievement-Gruppierung & Vergleich | Hoch | Offen |
| 🟢 **Niedrig** | History/Undo | Hoch | Offen |
| ⚪ **Optional** | UI-Feinschliff & Animationen | Variiert | Ideen |

---

## 🗑️ Aufräumarbeiten

### Deprecated-Projekte entfernen
- [ ] `Deprecated/` Ordner komplett löschen (SAM.Game + SAM.Picker)
- [x] SAM.Game und SAM.Picker aus SAM.sln entfernt
- [x] Alte WinForms-Referenzen bereinigt

---

## 🧹 Clean Code

### 🔴 P0 — Kritisch (Sicherheit & Funktionalität)

#### SSL-Zertifikatsvalidierung deaktiviert
- [x] `ServerCertificateCustomValidationCallback = (…) => true` in `SteamService.cs` entfernen
- [x] Nur für `gib.me`-Domain Ausnahme implementieren oder Zertifikat pinnen
- [x] Kein globales Deaktivieren aller SSL-Prüfungen

#### StatisticsPage Save-Button funktionslos
- [x] `// TODO: Implement actual save via SteamService` in `StatisticsPage.xaml.cs` umsetzen
- [x] Stats-Änderungen via `SteamUserStats` tatsächlich an Steam senden
- [x] Button deaktivieren oder Hinweis anzeigen solange nicht implementiert

---

### 🔴 P1 — SOLID & Duplikation

#### Concrete Cast bricht DI
- [x] `((SteamCallbackService)_callbackService).SetClient(…)` in `AchievementService.cs` entfernen
- [x] `SetClient(Client client)` Methode zum `ISteamCallbackService` Interface hinzufügen
- [x] Alle Interface-Casts in der gesamten Codebase suchen und eliminieren

#### ISteamService Leaking Implementation Details
- [ ] `Client?`, `SteamUserStats013?`, `SteamApps008?` Properties aus `ISteamService` entfernen
- [ ] Benötigte Operationen als High-Level-Methoden auf dem Interface exponieren
- [ ] Konsumenten von rohen API-Wrappern entkoppeln

#### Converters.cs 100% dupliziert
- [ ] Identische Converter zwischen `SAM.WinUI/Converters.cs` und `SAM.Manager/Converters.cs` konsolidieren
- [ ] Shared Converter-Klassen nach `SAM.Core` verschieben (oder neues UI-Shared-Projekt)
- [ ] `CountToVisibilityConverter` (nur in Manager) separat halten oder ebenfalls teilen

#### Schema-Parsing dreifach dupliziert
- [ ] `UserGameStatsSchema_{id}.bin` Parsing in eigene `SchemaParser`-Utility-Klasse extrahieren
- [ ] Duplikate in `AchievementService`, `DrmProtectionService`, `LibraryFetchService` ersetzen
- [ ] Gemeinsames Modell für Schema-Daten (Achievements, Stats, Permissions)

#### Hardcoded deutsche Strings umgehen Lokalisierung
- [ ] Alle deutschen Strings in Code-Behind durch `Loc.Get()` ersetzen:
  - `AchievementManagerPage.xaml.cs` — Dialog-Texte ("Ungespeicherte Änderungen", "Speichern", etc.)
  - `GamePickerPage.xaml.cs` — Fehlermeldungen ("SAM.Manager.exe nicht gefunden", etc.)
  - `StatisticsPage.xaml.cs` — Dialog-Texte
  - `DiagnosticsPage.xaml.cs` — Button-Texte und Fehlermeldungen
  - `SettingsPage.xaml.cs` — Dialog-Texte
- [ ] Entsprechende Einträge in `LocalizationService` für EN + DE anlegen
- [ ] DRM-Warning Strings in `AchievementManagerViewModel` und `LibraryFetchService` vereinheitlichen

---

### 🟡 P2 — Architektur & Code-Qualität

#### DiagnosticsPage God Class (703 Zeilen, kein ViewModel)
- [ ] `DiagnosticsViewModel` erstellen
- [ ] Logik extrahieren: System-Info, Steam-Status, Migration, Cache-Verwaltung
- [ ] Eigenen `IDiagnosticsService` für System-Info und Log-Zugriff einführen
- [ ] Code-Behind auf reine Event-Handler reduzieren

#### SettingsPage ohne ViewModel (259 Zeilen)
- [ ] `SettingsViewModel` erstellen
- [ ] Theme-, Sprach- und Library-Fetch-Logik in ViewModel verschieben
- [ ] Code-Behind auf reine UI-Interaktion beschränken

#### Dicke Code-Behind-Dateien ausdünnen
- [ ] `GamePickerPage.xaml.cs` (422 Zeilen): `LaunchSamManager()`-Logik in `IProcessLauncherService` extrahieren
- [ ] `MainWindow.xaml.cs` (467 Zeilen): Navigation-Logik aufteilen, TitleBar-Config auslagern
- [ ] `AchievementManagerPage.xaml.cs` (322 Zeilen): Confetti-Logik, CommandBar-Wiring, Dialog-Logik in ViewModel verschieben

#### async void bei Nicht-Event-Handlern
- [ ] `DiagnosticsPage.UpdateSteamStatus()` → `async Task` + Aufrufer anpassen
- [ ] `GamePickerPage.LaunchSamManager()` → `async Task` + Aufrufer anpassen
- [ ] `GamePickerPage.ShowErrorMessage()` → `async Task` + Aufrufer anpassen
- [ ] Alle `async void` Methoden prüfen — nur bei echten Event-Handlern beibehalten

#### Ungenutzte Injected Services
- [ ] `_imageCacheService` aus `AchievementManagerViewModel` entfernen (injected, nie verwendet)
- [ ] `_imageCacheService` aus `GamePickerViewModel` entfernen (injected, nie verwendet)
- [ ] `Stats`-Alias (`IEnumerable<StatModel> Stats => Statistics`) bereinigen — eins von beiden behalten

#### Steam CDN URLs zentralisieren
- [ ] `SteamCdnUrls`-Klasse in `SAM.Core/Utilities/` erstellen
- [ ] Alle verstreuten CDN-URLs konsolidieren (6+ Vorkommen in SteamService, AchievementService, AchievementManagerViewModel)
- [ ] Base-URLs als `const`-Felder, URL-Builder als statische Methoden

---

### 🟢 P3 — Feinschliff

#### ConfigureAwait(false) in Library-Code
- [ ] Alle `await`-Aufrufe in SAM.Core Services mit `.ConfigureAwait(false)` versehen
- [ ] Betrifft: `ImageCacheService`, `AchievementService`, `LibraryFetchService`, `GameCacheService`, `UserDataService`, `SettingsService`
- [ ] Nicht in UI-Code (WinUI, Manager) — dort wird der UI-Context benötigt

#### Magic Numbers durch benannte Konstanten ersetzen
- [ ] Permission-Bitmask `3` / `2` in `AchievementService` → `ProtectedPermissionMask` / `StatsPermissionMask`
- [ ] AppId `480` (Spacewar) → `SteamConstants.SpacewarAppId`
- [ ] Fenstergrößen `1280, 800` → Konstanten oder Settings
- [ ] Cache-Größen `100 * 1024 * 1024` → `MaxCacheSizeBytes` Konstante

#### Stille Exception-Behandlung beheben
- [ ] `DiagnosticsPage` — 3+ bare `catch`-Blöcke: `Log.Warning()` oder `Log.Exception()` hinzufügen
- [ ] `ImageCacheService` — bare `catch` in `UpdateFileAccessTime`, `LoadMetadataCache`, `EvictOldEntriesAsync`, `ClearCacheDirectory` loggen
- [ ] Konsistentes Muster: `catch (Exception ex) { Log.Exception(…, ex); }`

#### Excessive Debug-Logging entschärfen
- [ ] `MainWindow.xaml.cs` — 15+ `Log.Debug()` pro Methode reduzieren
- [ ] Nur relevante State-Changes loggen, nicht jeden einzelnen Schritt
- [ ] `Log.Verbose()` Level für detaillierte Init-Schritte einführen (falls benötigt)

#### Fake-Async bereinigen
- [ ] `AchievementService.GetAchievementsAsync()` — macht nur synchrone Arbeit mit `Task.FromResult`
- [ ] `AchievementService.GetStatisticsAsync()` — gleicher Fall
- [ ] Entweder synchrone Methoden oder echte async-Arbeit (z.B. async Schema-Parsing)

#### Fehleranzeige vereinheitlichen
- [ ] Einheitliches Muster für Fehleranzeige definieren: `NotificationBar` ODER `InfoBar` — nicht beides gemischt
- [ ] `AchievementManagerPage` verwendet `ErrorInfoBar` + `NotificationBar` → eines wählen
- [ ] Dokumentieren welches Pattern wann verwendet wird

#### Service Locator Anti-Pattern dokumentieren
- [ ] `App.GetService<T>()` in Code-Behind als bewusste WinUI-3-Einschränkung dokumentieren
- [ ] Kommentar: "WinUI 3 Pages unterstützen keine Constructor Injection — Service Locator ist Workaround"
- [ ] Langfristig: Custom PageFactory mit DI evaluieren

---

### 📊 Clean-Code-Metriken (Zielwerte)

| Metrik | Ist-Stand | Ziel |
|--------|-----------|------|
| Max. Zeilen pro Code-Behind | 703 (DiagnosticsPage) | < 150 |
| Max. Zeilen pro Methode | ~130 (LoadSchema) | < 50 |
| Duplizierter Code (Converter) | 100% identisch | 0% |
| Schema-Parsing Duplikation | 3× | 1× (zentral) |
| Hardcoded deutsche Strings | ~20+ Stellen | 0 (alle via Loc.Get) |
| async void (Nicht-Event) | 3 Methoden | 0 |
| Bare catch-Blöcke (ohne Logging) | ~7 Stellen | 0 |
| Ungenutzte injected Services | 2 Felder | 0 |
| Magic Numbers | ~8 Stellen | 0 |

---

## 🔜 Geplante Features

### Export/Import
- [ ] JSON-Export für Achievement-Backup (pro Spiel)
- [ ] Import-Funktion zur Wiederherstellung
- [ ] Batch-Export: Alle Spiele auf einmal sichern
- [ ] Profil-Sharing zwischen Benutzern (Export als `.sam`-Datei)

### Batch-Operationen
- [ ] Mehrfachauswahl im Game Picker (Checkboxen)
- [ ] Achievements für mehrere Spiele gleichzeitig unlock/lock
- [ ] Batch-Reset: Alle Achievements eines Spiels zurücksetzen

### History/Undo
- [ ] Protokollierung aller Änderungen in SQLite (Zeitstempel, vorher/nachher)
- [ ] Rückgängig-Funktion (letzte Aktion oder Session)
- [ ] History-Ansicht: Änderungsverlauf pro Spiel

### Achievement-Vergleich
- [ ] Vergleich mit Steam-Freunden (über Steam Web API)
- [ ] Globale Unlock-Raten anzeigen (% aller Spieler)
- [ ] Schwierigkeitsgrad-Indikator basierend auf globaler Rate

### Favoriten & Schnellzugriff
- [ ] Spiele als Favoriten markieren (⭐ Stern-Icon)
- [ ] „Zuletzt verwendet" — Schnellzugriff auf letzte 5 Spiele
- [ ] Drag & Drop in Favoritenliste
- [ ] Favoriten-Filter im Game Picker

### Steam-Integration
- [ ] Steam-Profilseite direkt öffnen (Store / Community Hub)
- [ ] Steam-Overlay-kompatible Benachrichtigungen
- [ ] SteamGridDB-Integration für fehlende Cover-Bilder
- [ ] Steam-Cloud-Status anzeigen (Cloud-Sync aktiv/inaktiv)

### Weitere Sprachen
- [x] Deutsch & Englisch (via ILocalizationService)
- [ ] Französisch
- [ ] Spanisch
- [ ] Russisch
- [ ] Chinesisch (vereinfacht)
- [ ] Community-Beiträge ermöglichen (JSON/RESX-Format dokumentieren)

---

## 🎨 UI/UX Verbesserungen

### Game Picker

#### Filter & Sortierung
- [ ] **Sortier-Dropdown** — Name, Spielzeit, Achievement-%, zuletzt gespielt
- [ ] **Schnellfilter-Chips** — „Kürzlich gespielt", „100% möglich", „Mit Achievements", „Favoriten"
- [ ] **Intelligente Suche** — Fuzzy-Matching, Aliase (z.B. „CSGO" → „Counter-Strike 2")
- [ ] **Zusammengesetzte Filter** — Typ + Sortierung + Suche kombinierbar

#### Visuelle Verbesserungen
- [ ] **Achievement-Fortschrittsbalken** auf jeder Game-Card (Detail-Ansicht)
- [ ] **Badges** — „100%", „Neu", „Kürzlich gespielt", „Geschützt"
- [ ] **Lazy Loading** mit Skeleton-Placeholders für Icons
- [ ] **Leerer-Zustand** — Illustration wenn keine Spiele gefunden

### Achievement Manager

#### Bulk-Aktionen
- [ ] **Mehrfachauswahl** mit separaten Checkboxen (unabhängig von Unlock-Status)
- [ ] **„Alle sichtbaren auswählen"** Button
- [ ] **Floating Action Bar** bei Auswahl (Lock / Unlock / Reset)

#### Bessere Übersicht
- [ ] **Gruppierung** — Nach DLC, Hidden/Visible, Unlock-Rate
- [ ] **Kompakt-Modus** — Nur Icons in Grid-Layout
- [ ] **Statistik-Header** — „47/82 freigeschaltet (57%)" als Progress-Ring

#### Interaktion
- [ ] **Swipe-Gesten** — Links = Lock, Rechts = Unlock (Touch-Geräte)
- [ ] **Kontextmenü** — Rechtsklick für Optionen (Details anzeigen, Steam-Seite öffnen)
- [ ] **Tastatur-Navigation** — Pfeiltasten + Space zum Togglen

### Statistiken-Seite
- [ ] **Visuelle Graphen** — Achievements über Zeit (Line Chart)
- [ ] **Vorher/Nachher-Vergleich** bei Stats-Änderungen
- [ ] **Warnungen** — Bei verdächtigen Werten (z.B. negative Werte, Overflow)
- [ ] **Reset-Button** pro Statistik (auf Standardwert zurücksetzen)

### Allgemein

#### Accessibility ♿
- [ ] **AutomationProperties** für alle interaktiven Elemente (Screenreader)
- [ ] **Hochkontrast-Modus** testen und unterstützen
- [ ] **Tastaturkürzel** — F5 Refresh, Ctrl+A Alle auswählen, Escape Abbrechen
- [ ] **Tab-Reihenfolge** optimieren für Keyboard-only Navigation
- [ ] **ARIA-Labels** für Achievement-Status und Progress

#### Quality of Life
- [x] **Einstellungs-Sync** — Zwischen SAM.WinUI und SAM.Manager (Shared-Settings über SQLite)
- [x] **Auto-Update** — Prüfung auf neue Version beim Start (GitHub Releases API)
- [ ] **Tray-Icon** — Minimieren in System-Tray mit Schnellzugriff
- [ ] **Multi-Monitor** — Fensterposition pro Monitor merken
- [ ] **Kommandozeilen-Argumente** — `--game <id>` für Direktstart

#### Themes & Personalisierung
- [x] **Akzentfarbe** wählbar (System oder benutzerdefiniert)
- [ ] **Eigene Header-Bilder** für Game-Cards (Custom Banner)
- [ ] **Kompakter Modus** — Reduzierte Abstände für kleine Bildschirme

---

## ⚡ Performance & Ladezeit

### Startup-Optimierung
- [x] **Lazy Service-Initialisierung** — Services erst bei Bedarf laden
- [x] **Splash Screen** — Visuelles Feedback während Steam-Initialisierung
- [x] **Parallel Loading** — Steam-Client und UI parallel initialisieren
- [x] **Cached Game List** — Letzte Spieleliste aus SQLite, im Hintergrund aktualisieren

### Game Picker Performance
- [x] **ItemsRepeater** mit Virtualisierung für große Bibliotheken (>500 Spiele)
- [x] **Inkrementelles Laden** — Erste 50 Spiele sofort, Rest im Hintergrund
- [x] **Image Lazy Loading** — Icons erst laden wenn sichtbar
- [x] **Placeholder-Images** — Generische Icons während Ladevorgang

### Achievement Manager Performance
- [x] **Pagination** — Bei >100 Achievements paginieren
- [x] **Deferred Icon Loading** — Achievement-Icons erst bei Scroll laden
- [x] **Background Prefetch** — Nächste Seite im Hintergrund vorladen

### Memory-Optimierung
- [x] **Image Disposal** — Nicht sichtbare Bilder aus Memory entfernen
- [x] **WeakReference Cache** — Bilder bei Memory-Druck freigeben
- [x] **Object Pooling** — ViewModels wiederverwenden statt neu erstellen

### Caching-Erweiterungen
- [x] **Offline-Modus** — App mit Cache-Daten nutzbar wenn Steam läuft aber kein Internet
- [x] **Cache-Verwaltung** — UI zum Anzeigen und Löschen von Cache-Daten
- [x] **Cache-Größe konfigurierbar** — Benutzerdefiniertes Limit für Icon-Cache

### Messbare Ziele
| Metrik | Ziel |
|--------|------|
| Cold Start → erste Interaktion | < 2s |
| Spieleliste vollständig geladen | < 500ms |
| Achievements eines Spiels laden | < 300ms |
| Maximaler RAM-Verbrauch | < 200MB |

---

## 🧪 Qualitätssicherung

### Ist-Stand: Test-Coverage-Analyse

| Metrik | Wert |
|--------|------|
| Testbare Klassen/Interfaces | 16+ |
| Davon mit Tests | 13 (6 Mock + 7 echte Impl.) |
| Komplett ungetestet | 3 Klassen |
| Geschätzte Methoden-Coverage | ~65% |
| Tests gegen echte Implementierungen | 7 (SteamErrorHelper, LocalizationService, SteamCallbackService, AchievementManagerViewModel, DrmProtectionService, GameCacheService, ViewModelBase) |
| Negative/Error-Path Tests | 13 |
| Concurrency-Tests | 0 |

#### Coverage-Matrix

| Klasse | Public Members | Getestet | Coverage |
|--------|---------------|----------|----------|
| GamePickerViewModel | 10 | 10 | ✅ 100% |
| ISettingsService (Mock) | 11 | 10 | ✅ 91% |
| IAchievementService (Mock) | 10 | 9 | ✅ 90% |
| AchievementManagerViewModel | 16 | 12 | 🟡 75% |
| IImageCacheService (Mock) | 8 | 5 | 🟡 63% |
| IUserDataService (Mock) | 6 | 6 | ✅ 100% |
| ISteamService | 12 | 0 | ❌ 0% |
| IDrmProtectionService | 2 | 0 | ❌ 0% |
| DrmProtectionService | 2 | 1 | 🟡 50% |
| ILibraryFetchService | 6 | 0 | ❌ 0% |
| ILocalizationService | 6 | 0 | ❌ 0% |
| ISteamCallbackService | 7 | 0 | ❌ 0% |
| SteamCallbackService | 11 | 1 | 🟡 9% |
| IGameCacheService | 10 | 0 | ❌ 0% |
| GameCacheService | 10 | 10 | ✅ 100% |
| ILegacyMigrationService | 6 | 0 | ❌ 0% |
| ViewModelBase | 6 | 6 | ✅ 100% |
| AppPaths | 13 | 0 | ❌ 0% |
| SteamErrorHelper | 3 | 3 | ✅ 100% |
| LocalizationService | 5 | 5 | ✅ 100% |

#### Hauptprobleme
1. **Alle Tests nutzen nur Mocks** — keine echte Service-Implementierung wird getestet
2. **MockGameCacheService existiert aber wird nirgends verwendet**
3. **Keine Error-Path Tests** — kein Test für Busy-Guard, CancellationToken, Exceptions
4. **DrmProtectionService ist Blindspot** — sicherheitskritisch, parst Binärdaten, 0 Tests

---

### 🔴 Tier 1 — Pure Logic, sofort testbar (Aufwand: Niedrig)

#### SteamErrorHelper Tests ✅ (30/30 bestanden)
- [x] `GetUserFriendlyMessage(ClientInitializeFailure)` — alle 6 Failure-Enums + Unknown + undefinierter Wert
- [x] `GetUserFriendlyMessage(ClientInitializeException)` — Delegation an Failure-Overload
- [x] `GetUserFriendlyMessage(Exception)` — alle 5 Pattern-Matches + Fallback + Case-Insensitivity + Edge Cases
- [x] Unbekannte Failures → Fallback-Nachricht

#### LocalizationService Tests ✅ (11/11 bestanden)
- [x] `GetString(key)` — bekannter Schlüssel in aktueller Sprache
- [x] `GetString(key)` — unbekannter Schlüssel → Fallback auf Englisch
- [x] `GetString(key)` — unbekannter Schlüssel in beiden Sprachen → Key zurückgeben
- [x] `GetString(key, params)` — Format-String mit Parametern
- [x] `GetString(key, params)` — `FormatException` bei falschen Parametern
- [x] `SetLanguage("de")` / `SetLanguage("en")` — Sprachwechsel
- [x] `SetLanguage("invalid")` — ungültige Sprache → Fallback
- [x] `AvailableLanguages` — gibt ["en", "de"] zurück
- [x] `LanguageChanged` Event wird ausgelöst
- [x] `Loc.Get(key)` statischer Helper — funktioniert nach `Initialize()`
- [x] `Loc.Get(key)` vor `Initialize()` → kein Crash

#### SteamCallbackService.TranslateResultCode Tests ✅ (22/22 bestanden)
- [x] Alle bekannten Steam Result Codes (1-108 ohne 4) → Uebersetzung vorhanden
- [x] Unbekannter Result Code → Fallback-String
- [x] `IsRetryableError` — retryable Codes (Busy, Timeout, etc.)
- [x] `IsRetryableError` — nicht-retryable Codes (AccessDenied, etc.)

#### AchievementManagerViewModel — fehlende Commands ✅ (13/13 bestanden)
- [x] `InvertAllCommand` — invertiert alle nicht-geschützten Achievements
- [x] `StoreStatsCommand` — sendet Änderungen (Mock verifizieren)
- [x] `ResetAllCommand` — setzt alle zurück
- [x] `RefreshCommand` — lädt Daten neu
- [x] `AchievementFilterType.Unlocked` — zeigt nur freigeschaltete
- [x] `AchievementFilterType.Locked` — zeigt nur gesperrte
- [x] `AchievementFilterType.Modified` — zeigt nur geänderte
- [x] `CompletionPercentage` Berechnung — 0/0, 5/10, 10/10
- [x] `ProtectedWarningMessage` — korrekter Text mit Zahlen

---

### 🟡 Tier 2 — Service-Implementierungen (Aufwand: Mittel)

#### DrmProtectionService Tests (sicherheitskritisch!) ✅ (6/6 bestanden)
- [x] Test-Schema-Binärdateien erstellen (protected + unprotected)
- [x] `CheckGameProtection(gameId)` — Spiel mit geschützten Achievements
- [x] `CheckGameProtection(gameId)` — Spiel ohne Schutz
- [x] `CheckGameProtection(gameId)` — fehlende Schema-Datei
- [x] `CheckGameProtection(gameId)` — korrupte/leere Datei
- [x] Permission-Bit-Analyse: `(permission & 3) != 0` für Achievements
- [x] Caching: zweiter Aufruf nutzt Cache

#### GameCacheService Tests (SQLite, In-Memory)
- [x] In-Memory SQLite (`Data Source=:memory:`) für Tests verwenden
- [x] `SaveGameAsync` + `GetGameAsync` — Round-Trip
- [x] `SaveGamesAsync` — Batch-Insert
- [x] `GetAllGamesAsync` — leere DB
- [x] `GetGamesForUserAsync` — nach SteamId filtern
- [x] `SearchGamesAsync` — Substring-Suche im Namen
- [x] `UpdateAchievementCountsAsync` — Zähler aktualisieren
- [x] `RemoveGameAsync` — Löschen
- [x] `ClearCacheAsync` — komplette DB leeren
- [x] `GetStatisticsAsync` — Cache-Größe und Einträge

#### ViewModelBase Tests ✅ (7/7 bestanden)
- [x] `ExecuteWithBusyAsync` — setzt `IsBusy=true` während Ausführung
- [x] `ExecuteWithBusyAsync` — verhindert doppelte Ausführung (Guard)
- [x] `ExecuteWithBusyAsync` — `OperationCanceledException` setzt keinen Fehler
- [x] `ExecuteWithBusyAsync` — andere Exception setzt `HasError=true`
- [x] `CancelOperations()` — löst CancellationToken aus
- [x] `SetError` / `ClearError` / `HasError` — State-Machine
- [x] `GetOperationCancellationToken` — neuer Token nach Cancel

#### AppPaths Tests (mit Temp-Verzeichnissen)
- [x] `GetUserPath(steamId)` — korrekter Pfad
- [x] `GetGamePath(steamId, gameId)` — korrekter Pfad
- [x] `GetAllUsers()` — liest vorhandene User-Ordner
- [x] `GetUserGames(steamId)` — liest vorhandene Game-Ordner
- [x] `SanitizeFileName(name)` — Sonderzeichen ersetzen
- [x] `SanitizeFileName` — Leerstring, null, nur Sonderzeichen
- [x] `CleanupOldLogs(maxAge)` — löscht alte Logs, behält neue

---

### 🟢 Tier 3 — Integration & Error-Paths (Aufwand: Hoch)

#### SettingsService — echte JSON-Persistenz
- [ ] `LoadAsync` — settings.json existiert → Werte laden
- [ ] `LoadAsync` — settings.json fehlt (Erststart) → Defaults
- [ ] `LoadAsync` — korruptes JSON → Defaults + kein Crash
- [ ] `SaveAsync` — schreibt gültiges JSON
- [ ] `SaveAsync` + `LoadAsync` Round-Trip — alle Properties
- [ ] `ResetToDefaults` + `SaveAsync` — Defaults persistent
- [ ] Concurrent `SaveAsync` — kein Data Race

#### UserDataService — echte Datei-Persistenz
- [ ] `SaveGameDataAsync` + `GetGameDataAsync` Round-Trip
- [ ] `GetGameDataAsync` — nicht existierender Pfad → null
- [ ] `GetAllGameDataAsync` — leeres Verzeichnis
- [ ] `DeleteGameDataAsync` — Datei wird gelöscht
- [ ] `SetCurrentUser` + `GetAllUsers` — Ordner-Struktur korrekt
- [ ] Concurrent Zugriff — kein Datenverlust

#### ImageCacheService — HTTP & File-Cache
- [ ] `GetImageAsync` — Mock-HTTP-Response → Datei gecacht
- [ ] `GetImageAsync` — HTTP 304 (Not Modified) → Cache-Hit
- [ ] `GetImageAsync` — HTTP-Fehler → null, kein Crash
- [ ] `GetImagesAsync` — paralleler Batch-Download
- [ ] LRU-Eviction bei Überschreitung von `MaxCacheSizeBytes`
- [ ] `ClearCache` — alle Dateien gelöscht
- [ ] `GetStatistics` — korrekte Zähler
- [ ] CancellationToken — bricht Download ab

#### LibraryFetchService — Orchestrierung
- [ ] `FetchAllGamesAsync` — lädt alle Spiele, ruft Sub-Services auf
- [ ] `FetchProgress` Event — Progress wird korrekt gemeldet
- [ ] `FetchAllGamesAsync` mit Cancellation — stoppt sauber
- [ ] Fehler bei einzelnem Spiel → andere werden weiter verarbeitet
- [ ] `IsFetching` Guard — verhindert doppelten Fetch

#### Error-Path Tests (übergreifend)
- [ ] Alle ViewModels: Verhalten wenn Service Exception wirft
- [ ] Alle ViewModels: `IsBusy`-Guard verhindert doppelte Ausführung
- [ ] Alle async Methoden: CancellationToken wird respektiert
- [ ] Alle Services: null-Parameter → `ArgumentNullException`

#### DI-Wiring Test
- [ ] `ServiceCollectionExtensions.AddSamCoreServices()` — alle Services registriert
- [ ] Alle Services auflösbar (keine fehlenden Dependencies)
- [ ] Singleton-Services liefern gleiche Instanz
- [ ] Transient-Services liefern unterschiedliche Instanzen

---

### 📊 Test-Metriken (Zielwerte)

| Metrik | Ist-Stand | Ziel |
|--------|-----------|------|
| Gesamte Test-Methoden | 165 | >150 |
| Klassen mit Coverage | 14 von 16 | 14 von 16 |
| Tests gegen echte Implementierungen | 105 | >40 |
| Error-Path Tests | 14 | >20 |
| Methoden-Coverage (geschätzt) | ~66% | >80% |
| Ungetestete kritische Services | 9 | 2 (SteamService, SteamCallbackService-Loop) |

> **Hinweis:** `SteamService` und der Callback-Loop von `SteamCallbackService` sind schwer
> testbar (native `steam_api.dll`). Nur die Pure-Logic-Teile (URL-Generierung, XML-Parsing,
> `TranslateResultCode`) können unit-getestet werden.

### Tests erweitern (Infrastruktur)
- [ ] Integration-Tests für Steam-API-Mocking (End-to-End)
- [ ] UI-Tests mit WinAppDriver oder Appium
- [ ] Code-Coverage auf >80% erhöhen
- [ ] CI/CD-Pipeline (GitHub Actions) mit automatischen Tests

### Code-Qualität
- [ ] Statische Code-Analyse (Roslyn Analyzers, StyleCop)
- [ ] Performance-Benchmarks mit BenchmarkDotNet
- [ ] Memory-Leak-Detection in CI

---

## 🚀 Langfristige Ideen

### Plugin-System
- [ ] API für Community-Erweiterungen (z.B. Custom-Badges, Themes)
- [ ] Achievement-Presets laden/teilen („100% dieses Spiels")

### Cloud-Features
- [ ] Achievement-Snapshots online speichern (Optional, verschlüsselt)
- [ ] Sync zwischen mehreren PCs

### Weitere Plattformen
- [ ] Linux-Support via Avalonia UI (langfristig)
- [ ] CLI-Tool für Achievement-Management ohne GUI

### Analytics-Dashboard
- [ ] Persönliche Achievement-Statistiken über alle Spiele
- [ ] Spielzeit-Tracking und Trends
- [ ] „Nächste leichte Achievements" Empfehlungen basierend auf globaler Unlock-Rate

---

## ✅ Abgeschlossen (v8.0.0)

<details>
<summary>Erledigte Aufgaben anzeigen</summary>

### Architektur ✅
- WinUI 3 Rewrite mit Two-App-Architektur (SAM.WinUI + SAM.Manager)
- MVVM mit CommunityToolkit.Mvvm und Dependency Injection
- SAM.Core als Shared Library mit ViewModels, Services, Models
- SAM.UI.Shared Projekt entfernt und in SAM.Core konsolidiert

### Steam-Integration ✅
- ISteamCallbackService mit async/await und Retry-Logik (exponentielles Backoff)
- UserStatsReceived Callback mit allen Steam Result Codes
- DRM-Protection-Erkennung via Schema-Analyse
- Bibliothek-Sync (Force Fetch) mit Progress und Cancellation

### Async & Performance ✅
- CancellationToken konsequent in allen Services
- App-Lifetime CancellationTokenSource für Shutdown
- HTTP/2 Multiplexing, ETag/If-Modified-Since, Request Batching
- SQLite Cache (games.db), LRU Image-Cache (max 100MB)

### UI/UX ✅
- Mica Backdrop, Dark/Light/System Theme
- NavigationView mit Game Picker, Settings, Diagnostics, About
- 3 Ansichts-Optionen: Standard, Kompakt, Detail (mit Persistenz)
- Game-Filter: Alle / Spiele / Mods / DLCs / Demos
- ConfettiControl bei 100%, NotificationBar, Micro-Animationen
- Ctrl+S Tastaturkürzel für Speichern

### Lokalisierung ✅
- ILocalizationService mit Code-basierten Dictionaries
- Englisch (Standard) + Deutsch
- Sprachwahl in Settings mit Persistenz

### Qualitätssicherung ✅
- SAM.Core.Tests: 58 Tests (xUnit + Moq)
- ViewModel-Tests (26) + Service-Tests (32)
- try-catch für alle async void Methoden
- SteamErrorHelper für benutzerfreundliche Fehlermeldungen
- Nullable Reference Types, `ArgumentNullException.ThrowIfNull()`

### Code-Qualität ✅
- LoggingService → `Log.cs` konsolidiert
- Typed HttpClient via DI
- Ungenutzte NuGet-Pakete entfernt
- .csproj Dateien aufgeräumt
- Alle Projekte auf .NET 10.0

</details>

---

*Letzte Aktualisierung: 2026-02-11*