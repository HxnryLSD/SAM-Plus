# Steam Achievement Manager Plus (SAM-Plus)

<p align="center">
  <strong>🎮 Modern Steam Achievement Management</strong><br>
  <em>Unlock, reset achievements and edit statistics – with a sleek Dark Mode UI</em>
</p>

---

## 🖼️ Preview

<p align="center">
  <img src="https://github.com/HxnryLSD/SAM-Plus/blob/master/PreviewImg/SAM.Picker.webp?raw=true" alt="Game Picker" width="600"/><br>
  <em>Game Picker – Browse and select from your Steam library</em>
</p>

<p align="center">
  <img src="https://github.com/HxnryLSD/SAM-Plus/blob/master/PreviewImg/SAM.Game.webp?raw=true" alt="Achievement Manager" width="600"/><br>
  <em>Achievement Manager – Unlock or reset achievements with one click</em>
</p>

---

## ✨ Features

### 🖥️ Modern Dark Theme UI
- **Borderless Window Design** with custom Title Bar
- **Dark Mode** throughout – no white elements
- **Smooth Scrolling** with momentum effect
- **Responsive Layout** – freely resizable window
- **Custom Scrollbars** in Store design

### 🎯 Achievement Management
- View all achievements for a game
- **Unlock or reset** individually or all at once
- Display unlock timestamps
- Automatically load achievement icons

### 📊 Statistics Editor
- View and edit game statistics
- Supports integer and float values
- Save changes with one click

### ⚡ Performance
- **.NET 8.0** – faster than .NET Framework
- **Async Icon Loading** – no UI freezes
- **Parallel Downloads** – icons load simultaneously
- **Bitmap Pool** – efficient memory management

---

## 📋 Requirements

| Component | Version | Note |
|-----------|---------|------|
| **Windows** | 10 / 11 | x86 or x64 |
| **Steam Client** | Latest | Must be running, user logged in |
| **.NET Runtime** | [8.0 Desktop (x86)](https://dotnet.microsoft.com/download/dotnet/8.0) | **x86 version required!** |

> ⚠️ **Important:** The x86 version of the .NET Runtime is mandatory, as Steam native DLLs are only available as 32-bit.

---

## 🚀 Installation

### Option 1: Download Release
1. Download the latest release from the [Releases page](../../releases)
2. Extract the ZIP
3. Run `SAM.Picker.exe`

### Option 2: Build Yourself
```powershell
# Clone repository
git clone https://github.com/username/SAM-Plus.git
cd SAM-Plus

# Create release build
dotnet publish -c Release -r win-x86 --self-contained false

# Output: upload\SAM.Picker.exe
```

---

## 🎮 Usage

1. **Start Steam** and log in
2. **Run SAM.Picker.exe**
3. Select a game from the list (double-click or Enter)
4. In the Achievement Manager:
   - ✅ Checkbox = Unlock achievement
   - ❌ Checkbox = Reset achievement
   - Click **Commit** to save changes

### Keyboard Shortcuts

| Key | Function |
|-----|----------|
| `Enter` | Open game |
| `Escape` | Close window |
| `Ctrl+F` | Focus search |

---

## 🏗️ Project Structure

```
SAM-Plus/
├── SAM.API/                 # Steam API Wrapper
│   ├── Client.cs            # Main client for Steam communication
│   ├── NativeWrapper.cs     # P/Invoke for steam_api.dll
│   ├── StoreThemeColors.cs  # Dark Theme color definitions
│   ├── StoreTitleBar.cs     # Custom Borderless Title Bar
│   ├── StoreScrollBar.cs    # Custom Dark Scrollbar
│   └── Wrappers/            # Interface wrappers for Steam APIs
│
├── SAM.Game/                # Achievement Manager
│   ├── Manager.cs           # Main window with Owner-Draw
│   └── Stats/               # Achievement & Statistics classes
│
├── SAM.Picker/              # Game Picker
│   ├── GamePicker.cs        # Game selection with Smooth Scrolling
│   └── GameInfo.cs          # Game data model
│
└── upload/                  # Build output
```

---

## 🛠️ Technical Details

### UI Architecture
- **WinForms** with Custom Owner-Draw for all controls
- **Borderless Window** with manual resize handling
- **Win32 P/Invoke** for scrollbar management
- **Double Buffering** to prevent flicker

### Steam Integration
- Native `steam_api.dll` via P/Invoke
- Callbacks for achievement updates
- Icon download via Steam CDN

### Changes Compared to Original SAM
| Area | Original | SAM-Plus |
|------|----------|----------|
| Framework | .NET Framework 4.8 | .NET 8.0 |
| HTTP | WebClient | HttpClient (async) |
| UI | Standard WinForms | Custom Dark Theme |
| Icons | Sync Download | Async Parallel |
| Window | Standard Border | Borderless Custom |

---

## 📝 Known Limitations

- **VAC-protected games:** Changes may result in VAC bans
- **Server-side achievements:** Some achievements are validated server-side
- **Online statistics:** Changes may be reset in online games

> ⚠️ **Disclaimer:** Use at your own risk. The developers assume no responsibility for account bans or other consequences.

---

## 📜 License

This project is licensed under the terms in [LICENSE.txt](LICENSE.txt).

### Attributions

- **Original SAM** by [gibbed](https://github.com/gibbed)
- **Icons:** [Fugue Icons](https://p.yusukekamiyamane.com/) by Yusuke Kamiyamane
- **UI Design** inspired by Steam Store

---

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first.

1. Create a fork
2. Create a feature branch (`git checkout -b feature/MyFeature`)
3. Commit your changes (`git commit -m 'Added feature'`)
4. Push the branch (`git push origin feature/MyFeature`)
5. Open a Pull Request
