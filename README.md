# СИНОЧКУ GAMES™

A custom Steam-style Windows game launcher for the СИНОЧКУ meme universe.

> **v2.0.0 is the stable public launcher.** It also contains the bridge updater needed to move Beta-channel users to v3 prereleases when they are published.

## Current games

- **СИНОЧКУ 2** — custom-branded Counter-Strike 2 launched through SteamEdit.

The v2 game registry is data-driven, so future СИНОЧКУ games can be added without hard-wiring new navigation pages.

## v2.0.0 highlights

- Steam-style Library, Game, and Settings pages
- Search, sorting, Favorites / Installed filters, and recently played ordering
- Data-driven game definitions and process detection
- Steam + multi-library auto-detection
- SteamEdit pre-launch hook
- First-run setup wizard
- Live tracked playtime while the launcher is open
- Per-user Steam custom artwork overrides with embedded fallback art
- Appearance, Steam, Games, Updates, and About settings
- Stable / Beta update channels
- v2 → v3 migration bridge: Stable waits for final v3 releases; Beta may opt into v3 prereleases
- Automatic and manual GitHub release checks
- Release notes in update prompts
- Updater backup/recovery and SHA-256 verification
- Logs and copyable diagnostics
- Safe LocalAppData custom-game manifests
- Automated tests and Windows CI
- Self-contained single-file Windows builds

## Repository layout

- `src/SinochkuGames/` — v2 launcher
- `tests/SinochkuGames.Tests/` — v2 tests
- `docs/V2_ARCHITECTURE.md` — architecture notes
- `docs/V2_ROADMAP.md` — current roadmap
- `docs/CUSTOM_GAMES.md` — custom game manifest format
- root `Program.cs` + `build.ps1` — v1.x launcher kept during the v2 transition

## Build v2

```powershell
dotnet test tests/SinochkuGames.Tests/SinochkuGames.Tests.csproj -c Release

dotnet publish src/SinochkuGames/SinochkuGames.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o dist/v2
```

GitHub Actions runs the same validation on Windows for v2 changes.

## Local data

Per-user settings, logs, updater staging files, tracked playtime, and optional custom-game manifests are stored below:

`%LocalAppData%\SinochkuGames`

## Branding note

**СИНОЧКУ GAMES™** uses ™ as an informal brand mark. It does **not** mean the name is a registered trademark.

This project is not affiliated with Valve or Steam.
