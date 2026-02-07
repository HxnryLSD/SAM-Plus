# Roadmap – Steam Achievement Manager (SAM-Plus)

Übersicht aller geplanten, laufenden und abgeschlossenen Features.  
Legende: ✅ Abgeschlossen | 🔴 Hohe Priorität | 🟡 Mittlere Priorität | 🟢 Niedrige Priorität

---

## 🔴 Hohe Priorität (Stabilität & Modernisierung)

### Stabilität & Fehlerbehandlung
- [ ] **Globale Fehlerbehandlung** - Globales Exception Handling (`Application.ThreadException`, `UnhandledException`) implementieren, um Abstürze ohne Log zu verhindern
- [ ] **Ressourcen-Cleanup (IDisposable)** - Korrektes Disposing von Netzwerk-Clients (`WebClient`, `IconDownloader`) beim Schließen von Fenstern
- [ ] **Logging-System** - Einführung eines Logging-Frameworks (z.B. Serilog oder NLog) für Datei-Logs bei Fehlern

### Technische Modernisierung
- [x] **Migration auf .NET 8** - Upgrade des Projekts von .NET Framework 4.8 auf .NET 8 für Performance und neue C#-Features
- [x] **Netzwerk-Layer Erneuerung** - Veralteten `WebClient` durch `HttpClient` ersetzen (Problemvermeidung bei Timeouts/Proxies)
- [x] **Async/Await Pattern** - Refactoring von blockierenden Calls und `BackgroundWorker` hin zu modernem `async`/`await` Task-Pattern

---

## 🟡 Mittlere Priorität (Architektur & Wartbarkeit)

### Code-Struktur
- [ ] **Konfiguration auslagern** - Hardcodierte URLs (z.B. `gib.me/sam/games.xml`) in eine `appsettings.json` oder `App.config` extrahieren
- [ ] **Typsicherheit im Binding** - Ersetzen von "Magic Strings" im DataBinding (z.B. `"DisplayName"`) durch `nameof()`-Operatoren
- [ ] **Dependency Injection (DI)** - Einführung eines simplen DI-Containers für Services (z.B. `SteamClient`, `LogService`), statt Weitergabe per Konstruktor

### Qualitätssicherung & Build
- [ ] **Unit Tests** - Erstellung einer Test-Suite (xUnit/NUnit) für die `SAM.API` Wrapper und Logik-Klassen (ohne UI)
- [ ] **GitHub Actions CI** - Ersetzen des alten AppVeyor-Builds durch moderne GitHub Actions Workflows (Build & Release)
- [ ] **Code-Analyse** - Aktivierung von strengeren Roslyn-Analyzers (NetAnalyzers) zur Code-Qualitätssicherung

### Feature-Erweiterungen
- [ ] **Sicherheits-Abfragen** - Bestätigungs-Dialoge ("Hold to Confirm") für kritische Aktionen wie "Unlock All" oder "Reset All"
- [ ] **Statistik-Backup** - Automatisches lokales Backup der `UserStats` als JSON vor jeder Änderung

---

## 🟢 Niedrige Priorität (UX & Optimierung)

### UI / UX Verbesserungen
- [ ] **High DPI Awareness** - Unterstützung für Per-Monitor DPI Skalierung (scharfe Schrift auf 4K-Monitoren)
- [ ] **Dark Mode / Theming** - Implementierung eines dunklen Farbschemas für `GamePicker` und `Manager`
- [ ] **Erweiterte Suche/Filter** - Im `Manager`: Filtern nach "Gesperrt", "Freigeschaltet" oder versteckten Achievements
- [ ] **Bessere Ladeindikatoren** - Moderne Spinner/Skeleton-Loading statt blockierender UI beim Laden von Icons

### Code-Kosmetik
- [ ] **EditorConfig** - Standardisierung von Formatierung (Tabs/Spaces, Braces) über `.editorconfig`
- [ ] **File-Scoped Namespaces** - Umstellung auf die kürzere Namespace-Syntax (C# 10+)
- [ ] **Symbol-Package** - Erstellung von NuGet-Symbolpaketen (`.snupkg`) für besseres Debugging

---

## 📋 Bekannte Probleme

| Problem | Schwere | Beschreibung |
|---------|---------|--------------|
| Absturz ohne Log | 🔴 Kritisch | App schließt sich kommentarlos bei unbehandelten Fehlern |
| ~~WebClient Obsolete~~ | ✅ Behoben | Ersetzt durch `HttpClient` |
| Hardcodierte URLs | 🟡 Mittel | Externe Ressourcen-URLs sind fest kompiliert (Single Point of Failure) |
| ~~UI Freezes~~ | ✅ Behoben | Async/Await implementiert |
| Binding Strings | 🟢 Niedrig | Fehleranfällige String-Referenzen für DataGrid-Spalten |

---

## 📊 Projekt-Status

| Bereich | Technologie | Details |
|---------|------------|---------|
| **Framework** | .NET 8.0 | ✅ Migriert von .NET Framework 4.8 |
| **UI** | Windows Forms (WinForms) | Klassische GDI+ Oberfläche, wenig Styling |
| **API Wrapper** | P/Invoke / Interop | Direkter Wrapper um `steam_api.dll` Interfaces |
| **Netzwerk** | HttpClient | ✅ Ersetzt veralteten WebClient |
| **Build** | MSBuild / dotnet CLI | Migration zu GitHub Actions geplant |
| **Architektur** | Code-Behind (Smart UI) | Soll Richtung MVP/MVVM oder Services refactored werden |
