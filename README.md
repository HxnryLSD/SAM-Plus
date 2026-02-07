# Steam Achievement Manager (SAM-Plus)

> Eine leichte, portable Anwendung zur Verwaltung von Achievements und Statistiken auf Steam.

## Überblick

Steam Achievement Manager (SAM) ermöglicht das Anzeigen, Freischalten und Zurücksetzen von Steam-Achievements sowie das Bearbeiten von Spielstatistiken.

**Voraussetzungen zum Ausführen:**
- Windows 10/11
- [Steam Client](https://store.steampowered.com/about/) (muss laufen, Benutzer eingeloggt)
- [.NET 8.0 Desktop Runtime (x86)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Änderungen (SAM-Plus)

| Feature | Beschreibung |
|---------|-------------|
| **.NET 8** | Upgrade von .NET Framework 4.8 für bessere Performance |
| **HttpClient** | Ersetzt veralteten `WebClient` für stabilere Downloads |
| **Async/Await** | Keine UI-Freezes mehr beim Laden von Icons |
| **Icons** | Fugue Icons Set |

## Build (Release)

```bash
# 1. Repository klonen
git clone <repository-url>
cd SAM-Plus

# 2. Release-Build erstellen (x86 ist Pflicht wegen Steam-DLLs)
dotnet publish -c Release -r win-x86 --self-contained false

# 3. Ausgabe-Verzeichnis
# -> bin\Release\net8.0-windows\win-x86\publish\
```

Starten Sie `SAM.Picker.exe` aus dem `publish`-Ordner.

## Projektstruktur

```
SAM-Plus/
├── SAM.API/       # Steam API Wrapper (P/Invoke)
├── SAM.Game/      # Achievement Manager UI
├── SAM.Picker/    # Game Picker UI
└── bin/           # Build-Ausgabe
```

## Roadmap

Siehe [PLANS.md](PLANS.md) für geplante Features und bekannte Probleme.

## Attribution

Icons: [Fugue Icons](https://p.yusukekamiyamane.com/) von Yusuke Kamiyamane.
