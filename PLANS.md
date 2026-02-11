# Steam Achievement Manager - Roadmap

## 📋 Aktuelle Priorisierung

| Priorität | Task | Aufwand | Status |
|-----------|------|---------|--------|
| 🔴 **Hoch** | Deprecated-Projekte entfernen | Niedrig | Offen |
| ✅ | Steam Callbacks implementieren | Mittel | ✅ Erledigt |
| ✅ | Async-Optimierungen | Mittel | ✅ Erledigt |
| ✅ | Unit-Tests erstellen | Hoch | ✅ Erledigt |
| 🟢 **Niedrig** | Export/Import Feature | Mittel | Offen |
| 🟢 **Niedrig** | Lokalisierung | Mittel | Offen |
| ⚪ **Optional** | UI/UX Verbesserungen | Variiert | Ideen |

---

## 🗑️ Aufräumarbeiten

### Deprecated-Projekte entfernen
- [ ] `Deprecated/` Ordner komplett löschen
- [x] SAM.Game und SAM.Picker aus SAM.sln entfernen *(Bereits entfernt)*
- [x] Alte WinForms-Referenzen bereinigen *(Keine WinForms in aktiven Projekten, nur in Deprecated/)*

---

## 🔧 Technische Verbesserungen

### ✅ Steam Callback-System (Erledigt)
- [x] ISteamCallbackService Interface erstellt
- [x] SteamCallbackService mit async/await Unterstützung
- [x] UserStatsReceived Callback implementiert
- [x] Retry-Logik für fehlgeschlagene API-Aufrufe (exponentielles Backoff)
- [x] Bessere Fehlerbehandlung bei Steam-Timeouts (konfigurierbar)
- [x] Alle Steam Result Codes übersetzt

### ✅ Async-Optimierungen (Erledigt)
- [x] CancellationToken konsequent durchgereicht
- [x] `InitializeServicesAsync` in App.xaml.cs refactored
- [x] App-Lifetime CancellationTokenSource für Shutdown-Handling
- [x] ViewModelBase mit CancellationToken-Unterstützung
- [x] Alle Service-Methoden unterstützen CancellationToken

### Lokalisierung
- [x] Resource-System einführen (Code-basierte Dictionaries mit ILocalizationService)
- [x] Englisch als Standardsprache
- [x] Deutsch als Option

---

## 🧪 Qualitätssicherung

### ✅ Unit-Tests (Erledigt)
- [x] SAM.Core.Tests Projekt erstellen (xUnit)
- [x] ISteamService und IAchievementService mocken (5 Mock-Services)
- [x] ViewModel-Tests für GamePicker, AchievementManager (26 Tests)
- [x] Service-Tests für Settings, ImageCache, UserData, Achievement (32 Tests)
- [x] 58 Tests total, alle bestanden

---

## 🔜 Geplante Features

### Export/Import
- [ ] JSON Export für Achievement-Backup
- [ ] Import-Funktion zur Wiederherstellung
- [ ] Profil-Sharing zwischen Benutzern

### Batch-Operationen
- [ ] Mehrfachauswahl im Game Picker
- [ ] Achievements für mehrere Spiele gleichzeitig

### History/Undo (Nice-to-have)
- [ ] Protokollierung von Änderungen
- [ ] Rückgängig-Funktion

### Achievement-Vergleich (Nice-to-have)
- [ ] Vergleich mit Steam-Freunden
- [ ] Globale Unlock-Raten anzeigen

---

## 🎨 UI/UX Verbesserungen

### Game Picker

#### Ansichts-Optionen
- [x] **Kompakte Listenansicht** - Nur Icon + Name, mehr Spiele auf einen Blick
- [x] **Detail-Ansicht** - Größere Cards mit Spielzeit, Achievement-Progress
- [x] **Ansicht speichern** - Letzte Auswahl merken (auswahl in Settings und design)

#### Filter & Sortierung
- [ ] **Schnellfilter-Chips** - "Kürzlich gespielt", "100% möglich", "Mit Achievements"
- [ ] **Sortier-Dropdown** - Name, Spielzeit, Achievement-%, zuletzt gespielt
- [ ] **Intelligente Suche** - Fuzzy-Matching, Aliase (z.B. "CSGO" → "Counter-Strike 2")

#### Visuelle Verbesserungen
- [ ] **Achievement-Fortschrittsbalken** auf jeder Game-Card
- [ ] **Badges** - "100%", "Neu", "Kürzlich gespielt"
- [ ] **Lazy Loading** mit Skeleton-Placeholders

### Achievement Manager

#### Bulk-Aktionen
- [ ] **Mehrfachauswahl** mit Checkboxen
- [ ] **"Alle sichtbaren auswählen"** Button
- [ ] **Floating Action Bar** bei Auswahl (Lock All / Unlock All)

#### Bessere Übersicht
- [ ] **Gruppierung** - Nach Kategorie, DLC, Schwierigkeit
- [ ] **Kompakt-Modus** - Nur Icons in Grid-Layout
- [ ] **Statistik-Header** - "47/82 freigeschaltet (57%)"

#### Interaktion
- [ ] **Swipe-Gesten** - Links = Lock, Rechts = Unlock
- [ ] **Kontextmenü** - Rechtsklick für Optionen
- [ ] **Tastatur-Navigation** - Pfeiltasten + Space zum Togglen

### Allgemein

#### Feedback & Animationen
- [x] **Toast-Verbesserungen** - Icon, Progress-Indicator (NotificationBar Control)
- [x] **Micro-Animations** - Bei Unlock/Lock sanfte Übergänge (ItemContainerTransitions)
- [x] **Konfetti-Animation** bei 100% Achievements (ConfettiControl)

#### Accessibility
- [ ] **Hochkontrast-Modus** unterstützen
- [ ] **Screenreader-Labels** für alle Steuerungen
- [ ] **Tastaturkürzel** - F5 Refresh, Ctrl+S Save, Ctrl+A Alle auswählen

#### Quality of Life
- [x] **Bibliothek-Sync (Force Fetch)** - Alle Spieldaten vorab laden (Achievements, DRM-Status)
- [ ] **Einstellungs-Sync** - Zwischen SAM.WinUI und SAM.Manager
- [ ] **"Zuletzt verwendet"** - Schnellzugriff auf letzte 5 Spiele
- [ ] **Drag & Drop** - Spiele in Favoritenliste ziehen
- [ ] **Steam-Link Integration** - Direkt zu Steam-Seite öffnen

### Statistiken-Seite

- [ ] **Visuelle Graphen** - Achievements über Zeit
- [ ] **Vergleich** - Vorher/Nachher bei Stats-Änderungen
- [ ] **Warnungen** - Bei verdächtigen Werten (VAC-Risiko)

---

## ⚡ Performance & Ladezeit

### Startup-Optimierung
- [ ] **Lazy Service-Initialisierung** - Services erst bei Bedarf laden
- [ ] **Splash Screen** - Visuelles Feedback während Steam-Initialisierung
- [ ] **Parallel Loading** - Steam-Client und UI parallel initialisieren
- [ ] **Cached Game List** - Letzte Spieleliste lokal cachen, im Hintergrund aktualisieren

### Game Picker Performance
- [ ] **Virtualisierung** - `ItemsRepeater` mit Virtualisierung für große Bibliotheken
- [ ] **Inkrementelles Laden** - Erste 50 Spiele sofort, Rest im Hintergrund
- [ ] **Image Lazy Loading** - Icons erst laden wenn sichtbar (IntersectionObserver-Pattern)
- [ ] **Thumbnail-Cache** - Kleinere Icons im Memory-Cache, volle Bilder on-demand
- [ ] **Placeholder-Images** - Generische Icons während Ladevorgang

### Achievement Manager Performance
- [ ] **Pagination** - Bei >100 Achievements paginieren statt alle laden
- [ ] **Deferred Icon Loading** - Achievement-Icons erst bei Scroll laden
- [ ] **Background Prefetch** - Nächste Seite im Hintergrund vorladen

### Netzwerk-Optimierung
- [x] **HTTP/2** - Multiplexing für parallele Icon-Downloads
- [x] **Conditional Requests** - ETag/If-Modified-Since für Cache-Validierung
- [x] **Image CDN** - Steam CDN URLs optimal nutzen (akamai)
- [x] **Request Batching** - Mehrere kleine Requests zusammenfassen

### Memory-Optimierung
- [ ] **Image Disposal** - Nicht sichtbare Bilder aus Memory entfernen
- [ ] **WeakReference Cache** - Bilder bei Memory-Druck freigeben
- [ ] **Object Pooling** - ViewModels wiederverwenden statt neu erstellen

### Caching-Strategie
- [x] **SQLite Cache** - Persistent Cache für Game-Metadaten (games.db)
- [x] **LRU Cache** - Least Recently Used für Icon-Cache (max 100MB)
- [ ] **Offline-Modus** - App auch ohne Internet nutzbar (cached data)

### Messbare Ziele
- [ ] **Cold Start < 2s** - App-Start bis erste Interaktion
- [ ] **Game List < 500ms** - Spieleliste vollständig geladen
- [ ] **Achievement Load < 300ms** - Achievements eines Spiels laden
- [ ] **Memory < 200MB** - Maximaler RAM-Verbrauch

---

## ✅ Abgeschlossen (v8.0.0)

<details>
<summary>Erledigte Aufgaben anzeigen</summary>

### Code-Qualität ✅
- SAM.UI.Shared Projekt entfernt
- LoggingService zu `Log.cs` in SAM.Core konsolidiert
- try-catch zu allen async void Methoden (7 Methoden)
- Logging zu stillen catch-Blöcken hinzugefügt
- SteamErrorHelper für benutzerfreundliche Fehlermeldungen
- `null!` Patterns durch `ArgumentNullException.ThrowIfNull()` ersetzt
- ImageCacheService: Typed HttpClient via DI

### Projekt-Struktur ✅
- Ungenutzte NuGet-Pakete entfernt (CommunityToolkit.WinUI.*)
- .csproj Dateien aufgeräumt
- SAM.API.csproj: Version 8.0.0, PropertyGroups konsolidiert

### Entscheidungen
- Debug-Logs bleiben erhalten (hilfreich für Fehlersuche)

</details>

---

*Letzte Aktualisierung: 2026-02-10*