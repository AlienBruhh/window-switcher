# Window Switcher

Window Switcher is a Windows desktop launcher that starts applications and toggles their main windows between foreground and minimized states. It can also assign system-wide hotkeys to each configured application.

## Requirements

- Windows 10 or later
- .NET 10 SDK to build from source

## Run

```powershell
dotnet run --project .\WindowToggleLauncher\WindowToggleLauncher.csproj
```

## Use

1. Select **Add Application** and choose an executable.
2. Use the edit action to set a display name, arguments, startup preference, and an optional hotkey.
3. Use **Toggle** to launch the application, bring it to the foreground, or minimize its main window.

Hotkeys take effect immediately when the configuration is saved. Supported examples include:

- `A` or `B` for a global single-key hotkey
- `Ctrl+Alt+A`
- Named WPF keys such as `F5` or `Escape`

Single-key hotkeys are global and consume that keypress, so the key will not be typed into the currently focused application. Windows-reserved or already-registered combinations cannot be used; the app reports registration failures after saving.

## Build

```powershell
dotnet build .\WindowToggleLauncher.sln
```

## Configuration

Application settings are saved per user under the Windows local application-data directory, so no machine-specific settings are committed to this repository.

## Project Structure
