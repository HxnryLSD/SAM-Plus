# Steam Achievement Manager Plus (SAM-Plus)

<p align="center">
  <strong>🎮 Moderne Steam Achievement Verwaltung</strong><br>
  <em>Achievements freischalten, zurücksetzen und Statistiken bearbeiten – mit schlankem Dark-Mode UI</em>
</p>

---

## ✨ Features

### 🖥️ Modernes Dark-Theme UI
- **Borderless Window Design** mit custom Title Bar
- **Dark Mode** durchgehend – keine weißen Elemente
- **Smooth Scrolling** mit Momentum-Effekt
- **Responsive Layout** – Fenster frei skalierbar
- **Custom Scrollbars** im Store-Design

### 🎯 Achievement Management
- Alle Achievements eines Spiels anzeigen
- **Einzeln oder alle** freischalten/zurücksetzen
- Unlock-Zeitstempel anzeigen
- Achievement-Icons automatisch laden

### 📊 Statistik-Editor
- Spielstatistiken anzeigen und bearbeiten
- Integer- und Float-Werte unterstützt
- Änderungen mit einem Klick speichern

### ⚡ Performance
- **.NET 8.0** – schneller als .NET Framework
- **Async Icon-Loading** – keine UI-Freezes
- **Parallele Downloads** – Icons laden gleichzeitig
- **Bitmap-Pool** – effizientes Memory-Management

---

## 📋 Voraussetzungen

| Komponente | Version | Hinweis |
|------------|---------|---------|
| **Windows** | 10 / 11 | x86 oder x64 |
| **Steam Client** | Aktuell | Muss laufen, Benutzer eingeloggt |
| **.NET Runtime** | [8.0 Desktop (x86)](https://dotnet.microsoft.com/download/dotnet/8.0) | **x86 Version erforderlich!** |

> ⚠️ **Wichtig:** Die x86-Version der .NET Runtime ist Pflicht, da Steam native DLLs nur als 32-bit vorliegen.

---

## 🚀 Installation

### Option 1: Release herunterladen
1. Neuestes Release von der [Releases-Seite](../../releases) herunterladen
2. ZIP entpacken
3. `SAM.Picker.exe` starten

### Option 2: Selbst bauen
```powershell
# Repository klonen
git clone https://github.com/username/SAM-Plus.git
cd SAM-Plus

# Release-Build erstellen
dotnet publish -c Release -r win-x86 --self-contained false

# Ausgabe: upload\SAM.Picker.exe
```

---

## 🎮 Verwendung

1. **Steam starten** und einloggen
2. **SAM.Picker.exe** ausführen
3. Spiel aus der Liste wählen (Doppelklick oder Enter)
4. Im Achievement Manager:
   - ✅ Checkbox = Achievement freischalten
   - ❌ Checkbox = Achievement zurücksetzen
   - **Commit** klicken um Änderungen zu speichern

### Tastenkürzel

| Taste | Funktion |
|-------|----------|
| `Enter` | Spiel öffnen |
| `Escape` | Fenster schließen |
| `Strg+F` | Suche fokussieren |

---

## 🏗️ Projektstruktur

```
SAM-Plus/
├── SAM.API/                 # Steam API Wrapper
│   ├── Client.cs            # Hauptclient für Steam-Kommunikation
│   ├── NativeWrapper.cs     # P/Invoke für steam_api.dll
│   ├── StoreThemeColors.cs  # Dark Theme Farbdefinitionen
│   ├── StoreTitleBar.cs     # Custom Borderless Title Bar
│   ├── StoreScrollBar.cs    # Custom Dark Scrollbar
│   └── Wrappers/            # Interface-Wrapper für Steam APIs
│
├── SAM.Game/                # Achievement Manager
│   ├── Manager.cs           # Hauptfenster mit Owner-Draw
│   └── Stats/               # Achievement & Statistik-Klassen
│
├── SAM.Picker/              # Game Picker
│   ├── GamePicker.cs        # Spielauswahl mit Smooth Scrolling
│   └── GameInfo.cs          # Spiel-Datenmodell
│
└── upload/                  # Build-Ausgabe
```

---

## 🛠️ Technische Details

### UI-Architektur
- **WinForms** mit Custom Owner-Draw für alle Controls
- **Borderless Window** mit manuellem Resize-Handling
- **Win32 P/Invoke** für Scrollbar-Management
- **Double Buffering** gegen Flicker

### Steam Integration
- Native `steam_api.dll` via P/Invoke
- Callbacks für Achievement-Updates
- Icon-Download über Steam CDN

### Änderungen gegenüber Original-SAM
| Bereich | Original | SAM-Plus |
|---------|----------|----------|
| Framework | .NET Framework 4.8 | .NET 8.0 |
| HTTP | WebClient | HttpClient (async) |
| UI | Standard WinForms | Custom Dark Theme |
| Icons | Sync Download | Async Parallel |
| Window | Standard Border | Borderless Custom |

---

## 📝 Bekannte Einschränkungen

- **VAC-geschützte Spiele:** Änderungen können zu VAC-Bans führen
- **Server-seitige Achievements:** Manche Achievements werden serverseitig validiert
- **Online-Statistiken:** Änderungen können bei Online-Spielen zurückgesetzt werden

> ⚠️ **Disclaimer:** Die Nutzung erfolgt auf eigene Gefahr. Die Entwickler übernehmen keine Verantwortung für Account-Sperren oder andere Konsequenzen.

---

## 📜 Lizenz

Dieses Projekt steht unter der Lizenz im [LICENSE.txt](LICENSE.txt).

### Attributionen

- **Original SAM** von [gibbed](https://github.com/gibbed)
- **Icons:** [Fugue Icons](https://p.yusukekamiyamane.com/) von Yusuke Kamiyamane
- **UI Design** inspiriert von Steam Store

---

## 🤝 Beitragen

Pull Requests sind willkommen! Für größere Änderungen bitte erst ein Issue öffnen.

1. Fork erstellen
2. Feature-Branch anlegen (`git checkout -b feature/MeinFeature`)
3. Änderungen committen (`git commit -m 'Feature hinzugefügt'`)
4. Branch pushen (`git push origin feature/MeinFeature`)
5. Pull Request öffnen
