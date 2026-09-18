# СИНОЧКУ GAMES™ v2

V2 is a clean rewrite of the launcher as a modular .NET 8 WinForms application.

## What's new

- Data-driven game registry — СИНОЧКУ 2 is game #1, not hard-wired UI state.
- Proper Library page and game detail pages.
- Real Settings page with persisted options.
- Steam + Steam library auto-detection.
- Game install detection by Steam AppID.
- SteamEdit discovery and per-user path configuration.
- GitHub Release update checks.
- Modern self-contained build pipeline using .NET 8.
- Split services/models/UI instead of one giant source file.

## Architecture

- `Models/` — game and settings data
- `Services/` — Steam, launching, updates, persistence, registry
- `UI/` — shell, library, settings, game pages

## Build

```powershell
dotnet publish v2/SinochkuGames.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

This branch is intentionally separate from the currently released v1.x launcher while v2 is being hardened.
