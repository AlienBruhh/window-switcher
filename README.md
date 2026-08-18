<div align="center">

# ⚡ DeskDeck
### Turn your Phone into a Free Touch Stream Deck & Windows App Switcher

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=flat&logo=windows)](https://microsoft.com/windows)
[![Mobile](https://img.shields.io/badge/Mobile-Android%20%7C%20iOS%20%7C%20PWA-3DDC84?style=flat&logo=android)](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/AlienBruhh/window-switcher/pulls)

**DeskDeck** is a lightweight, zero-latency open-source Windows desktop application that lets you instantly switch, launch, and focus desktop windows using **physical keyboard hotkeys** or from your **smartphone as a wireless touch macro pad (Stream Deck)** via instant QR code pairing.

[Quick Start](#-quick-start) • [Features](#-key-features) • [How it Works](#-how-it-works) • [Packaging .EXE](#-build--publish-single-file-exe) • [Contributing](#-contributing)

---

</div>

```text
  ┌────────────────────────┐                   ┌────────────────────────┐
  │      Android Phone     │                   │       Windows PC       │
  │ ┌────────────────────┐ │                   │ ┌────────────────────┐ │
  │ │  🌐 Chrome / PWA   │ │   Wi-Fi / LAN     │ │ ⚡ DeskDeck App    │ │
  │ │                    │ │ ◄───────────────► │ │                    │ │
  │ │ [VS Code] [Chrome] │ │  WebSocket / HTTP │ │ [1] -> VS Code     │ │
  │ │ [Spotify] [Discord]│ │                   │ │ [2] -> Chrome      │ │
  │ └────────────────────┘ │                   │ └────────────────────┘ │
  └────────────────────────┘                   └────────────────────────┘
```

---

## ✨ Key Features

- 📱 **Instant QR Code Pairing**: No IP typing, no port forwarding, no accounts, and no mobile app installation needed. Simply point your phone camera at the on-screen QR code.
- ⚡ **Zero-Latency Remote Control**: Tapping any application on your mobile browser immediately launches, restores, or minimizes that app on your PC.
- 🎨 **Real Application Icons**: Automatically extracts high-resolution icons directly from your Windows executables (`.exe`) and serves them to your mobile remote.
- 🔄 **Real-Time Live Synchronization**: Any changes made in the desktop app (adding apps, editing, window focus state changes) are pushed in real time via WebSockets to connected phones.
- ⌨️ **Physical Keyboard Hotkeys**: Global single-key (`1`, `2`, `F5`) or key combinations (`Ctrl+Alt+S`) for lightning-fast desktop window switching.
- 🔒 **Local & Secure**: Works 100% locally over your Wi-Fi router. Employs cryptographically secure pairing tokens and session tokens. No external cloud or third-party servers.
- 📲 **Installable PWA**: Add DeskDeck to your phone's Home Screen ("Add to Home screen") to run full-screen just like a native mobile app.
- 📳 **Haptic Feedback**: Tactile vibration on button tap for a responsive physical deck feel.
- 🛡️ **Zero-Admin Networking**: Uses embedded ASP.NET Core Kestrel in-process sockets (`0.0.0.0:8765`), avoiding Windows URL reservation (`netsh http add urlacl`) and admin elevation.

---

## 🚀 Quick Start

### Prerequisites
- Windows 10 or Windows 11 (64-bit)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (if building from source)
- Both your PC and phone connected to the **same Wi-Fi / Local Network**

### Running from Source
```powershell
# Clone the repository
git clone https://github.com/AlienBruhh/window-switcher.git
cd window-switcher

# Run the desktop app
dotnet run --project .\WindowToggleLauncher\WindowToggleLauncher.csproj
```

### Running the Published `.exe`
Download or build the single-file executable and run:
```powershell
.\publish\DeskDeck.exe
```

---

## 📖 How to Use

1. **Add Applications**: Click **➕ Add App** and select any `.exe` (e.g., Chrome, VS Code, Spotify, Blender, Steam).
2. **Configure Hotkeys (Optional)**: Click **Edit** to give it a custom name, arguments, or assign a keyboard hotkey (e.g. `1`, `Ctrl+Shift+D`).
3. **Connect Your Phone**:
   - The desktop UI displays a **QR Code** with your local Wi-Fi URL.
   - Open your Android / iPhone camera and scan the QR code.
   - The mobile web remote opens instantly with all your configured apps, real icons, and live status.
4. **Switch & Control**: Tap any app button on your phone to bring that application to the front!

---

## 📦 Build / Publish Single-File .EXE

You can publish a self-contained single-file `.exe` that runs on any Windows PC without needing the .NET runtime installed:

```powershell
dotnet publish .\WindowToggleLauncher\WindowToggleLauncher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish
```

The output executable will be created in `.\publish\WindowToggleLauncher.exe`.

---

## 🧪 Running Automated Tests

DeskDeck includes a comprehensive xUnit integration and unit test suite verifying networking, QR generation, token security, REST APIs, and WebSocket push sync:

```powershell
dotnet test
```

---

## 🌐 Windows Firewall & Networking

- DeskDeck listens on port `8765` (or automatically finds the next available port).
- On first launch, Windows Defender Firewall may display a prompt: click **Allow Access on Private Networks**.
- If needed, you can manually allow the inbound port via PowerShell (Run as Administrator):
  ```powershell
  New-NetFirewallRule -DisplayName "DeskDeck Remote Control" -Direction Inbound -LocalPort 8765 -Protocol TCP -Action Allow
  ```

---

## 🛠️ Architecture & Tech Stack

- **Desktop GUI**: C# / WPF (Windows Presentation Foundation), Modern Dark Theme
- **Target Framework**: .NET 10 Windows (`net10.0-windows`)
- **Embedded Web Server**: ASP.NET Core Kestrel Minimal APIs & WebSockets
- **QR Code Engine**: QRCoder
- **Mobile Client**: HTML5, Vanilla CSS3 (Glassmorphism & Mobile Responsive Grid), Vanilla ES6 JavaScript, Web Manifest (PWA)
- **Windows Interop**: Native Win32 API (`User32.dll`, `RegisterHotKey`, `SetForegroundWindow`, `ShowWindowAsync`)

---

## 🏷️ Tags & Keywords

`windows-app-switcher` • `stream-deck-alternative` • `macro-pad` • `phone-remote-pc` • `qr-code-remote` • `android-pc-controller` • `windows-hotkey-manager` • `pwa-stream-deck` • `dotnet10` • `wpf-app` • `kestrel-embedded` • `local-network-remote`

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
