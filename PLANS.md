# 🗺️ SAM-Plus Roadmap

> **Entwicklungsplan und Feature-Tracking für Steam Achievement Manager Plus**

---

## 📊 Projekt-Übersicht

| Status | Bedeutung |
|--------|-----------|
| ✅ | Abgeschlossen |
| 🚧 | In Arbeit |
| 📋 | Geplant |
| ❌ | Abgebrochen / Nicht möglich |

**Aktuelle Version:** 7.0  
**Framework:** .NET 8.0  
**UI:** Windows Forms mit Custom Dark Theme

---

## 🛡️ Sicherheit & VAC – FAQ

<details>
<summary><strong>Wird SAM von Steam/VAC erkannt?</strong></summary>

### Kurze Antwort: Nein

SAM ist seit über 15 Jahren in Benutzung. Valve hat nie Maßnahmen dagegen ergriffen.

**Warum ist SAM sicher?**
- Nutzt die **offizielle Steam API** (`steamclient.dll`)
- Keine Hacks, Injections oder Code-Modifikationen
- Achievements sind **nicht VAC-geschützt**
- `SetAchievement()` und `StoreStats()` sind **legitime Steamworks-Funktionen**

**Mögliche Einschränkungen:**
| Situation | Auswirkung |
|-----------|------------|
| Serverseitige Validierung (MMOs) | Achievement wird zurückgesetzt |
| Leaderboard-Checks | Achievement zählt nicht für Rankings |
| Spielzeit-Prüfung | Offensichtlich unrealistische Zeiten sichtbar |

</details>

---

## ✅ Abgeschlossene Features

### Kern-Modernisierung
| Feature | Beschreibung |
|---------|-------------|
| ✅ **.NET 8.0 Migration** | Upgrade von .NET Framework 4.8 |
| ✅ **HttpClient** | Ersetzt veralteten WebClient |
| ✅ **Async/Await** | Keine UI-Freezes mehr |
| ✅ **Globales Error Handling** | Crash-Schutz mit Logging |
| ✅ **Dependency Injection** | ServiceLocator Pattern |
| ✅ **Konfiguration** | appsettings.json für URLs |

### UI-Modernisierung
| Feature | Beschreibung |
|---------|-------------|
| ✅ **Dark Theme** | Durchgängig dunkles Design |
| ✅ **Borderless Windows** | Custom Title Bar mit Drag & Close |
| ✅ **Custom Scrollbars** | Store-Style Scrollbars |
| ✅ **Smooth Scrolling** | Momentum-basiertes Scrollen |
| ✅ **Responsive Layout** | Frei skalierbare Fenster |
| ✅ **Owner-Draw Controls** | ListView, TabControl, Checkboxes |
| ✅ **Keyboard Shortcuts** | F5, Ctrl+S, Ctrl+F, Escape |

### Performance
| Feature | Beschreibung |
|---------|-------------|
| ✅ **Icon Caching** | Lokaler Cache in %LOCALAPPDATA% |
| ✅ **Parallel Downloads** | Max 5 gleichzeitige Icon-Downloads |
| ✅ **Bitmap Pool** | Wiederverwendung von Bitmaps |
| ✅ **Debounced Search** | 150ms Verzögerung |
| ✅ **Game List Cache** | JSON-Cache mit 24h TTL |
| ✅ **ReadyToRun** | AOT-Kompilierung |
| ✅ **TieredPGO** | Profile-Guided Optimization |
| ✅ **Virtual Mode** | ListView für große Listen |

---

## 📋 Geplante Features

### 🔴 Hohe Priorität

#### Qualitätssicherung
- [ ] **Unit Tests** – xUnit Tests für SAM.API Wrapper
- [ ] **GitHub Actions CI** – Automatisierte Builds
- [ ] **Code-Analyse** – Roslyn Analyzers aktivieren

#### Sicherheits-Features
- [ ] **Bestätigungs-Dialoge** – "Hold to Confirm" für kritische Aktionen
- [ ] **Statistik-Backup** – Automatisches JSON-Backup vor Änderungen

### 🟡 Mittlere Priorität

#### Feature-Erweiterungen
| Feature | Beschreibung |
|---------|-------------|
| 📋 **Achievement Import/Export** | JSON/XML Export für Backup |
| 📋 **Batch-Operationen** | Mehrere Spiele gleichzeitig bearbeiten |
| 📋 **Vorlagen-System** | Achievement-Sets als Templates speichern |
| 📋 **Sortier-Optionen** | Nach Name, Seltenheit, Datum sortieren |
| 📋 **Spiel-Favoriten** | Schnellzugriff auf häufige Spiele |

#### Sicherheits-Features
| Feature | Beschreibung |
|---------|-------------|
| 📋 **Realistische Unlock-Zeiten** | Zufällige Verzögerung (5-30 Min) |
| 📋 **Spielzeit-Warnung** | Hinweis bei unrealistischer Zeit |
| 📋 **Safe Mode** | Nur "erreichbare" Achievements freigeben |
| 📋 **Profil-Vorschau** | Preview wie Steam-Profil aussehen wird |

### 🟢 Niedrige Priorität

#### UI-Verbesserungen
- [ ] **Achievement-Statistik** – Completion-Rate, seltenste Achievements
- [ ] **Mehrsprachigkeit** – DE, EN, FR, ES Übersetzungen
- [ ] **Bessere Ladeindikatoren** – Skeleton Loading

#### Code-Qualität
- [ ] **EditorConfig** – Einheitliche Formatierung
- [ ] **File-Scoped Namespaces** – C# 10+ Syntax
- [ ] **Records** – Immutable DTOs

---

## 🚀 Zukunftsideen

| Feature | Aufwand | Status |
|---------|---------|--------|
| **Steam Deck Support** | 🟡 Mittel | Nicht geplant |
| **Steam Web API** | 🟡 Mittel | Evaluierung |
| **CLI-Modus** | 🟡 Mittel | Interessant |
| **Plugin-System** | 🔴 Hoch | Langfristig |
| **Cloud Sync** | 🔴 Hoch | Nicht geplant |

---

## ❌ Nicht Möglich

| Feature | Grund |
|---------|-------|
| **Trimmed Publish** | Inkompatibel mit WinForms (NETSDK1175) |
| **Zeitstempel ändern** | Steam API erlaubt das nicht |
| **VAC umgehen** | SAM tangiert VAC nicht |

---

## 🏗️ Technische Architektur

```
┌─────────────────────────────────────────────────────────┐
│                    SAM.Picker.exe                       │
│              (Game Selection UI)                        │
├─────────────────────────────────────────────────────────┤
│                    SAM.Game.exe                         │
│            (Achievement Manager UI)                     │
├─────────────────────────────────────────────────────────┤
│                     SAM.API.dll                         │
│    ┌─────────────┬──────────────┬──────────────┐       │
│    │ Client.cs   │ ThemeManager │ ServiceLocator│       │
│    ├─────────────┼──────────────┼──────────────┤       │
│    │ Callbacks/  │ Wrappers/    │ Types/       │       │
│    └─────────────┴──────────────┴──────────────┘       │
├─────────────────────────────────────────────────────────┤
│                   steam_api.dll                         │
│              (Valve Native Library)                     │
└─────────────────────────────────────────────────────────┘
```

### Komponenten

| Modul | Verantwortung |
|-------|---------------|
| **SAM.Picker** | Spielauswahl, Smooth Scrolling, Suche |
| **SAM.Game** | Achievement-Liste, Stats-Editor, Commit |
| **SAM.API** | Steam-Kommunikation, Theme, Services |

### Wichtige Klassen

| Klasse | Funktion |
|--------|----------|
| `Client` | Steam-Verbindung, Callbacks |
| `StoreThemeColors` | Farbdefinitionen für Dark Theme |
| `StoreTitleBar` | Custom Borderless Title Bar |
| `StoreScrollBar` | Custom Dark Scrollbar |
| `ServiceLocator` | DI Container |
| `AppConfig` | Konfiguration aus appsettings.json |

---

## 📝 Changelog

### Version 7.0 (Aktuell)
- ✨ Komplett neues Dark Theme UI
- ✨ Borderless Windows mit Custom Title Bar
- ✨ Smooth Scrolling mit Momentum
- ✨ Custom Scrollbars
- ✨ Owner-Draw für alle Controls
- ⚡ Performance-Optimierungen
- 🐛 Zahlreiche Bugfixes

### Version 6.x
- 🔄 Migration auf .NET 8.0
- ⚡ Async/Await Pattern
- ⚡ HttpClient statt WebClient
- ✨ Icon Caching

---

## 🤝 Beitragen

Interessiert an einem Feature? 

1. Issue erstellen mit Feature-Request
2. Fork → Feature Branch → Pull Request
3. Code-Review abwarten

**Priorität von Issues:**
- 🐛 Bugs → Höchste Priorität
- 🔒 Sicherheit → Hohe Priorität  
- ✨ Features → Nach Diskussion
