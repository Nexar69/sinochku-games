# СИНОЧКУ GAMES™

A Steam-inspired Windows game launcher and social platform that somehow grew out of one Counter-Strike 2 rename joke.

## Current game

- **СИНОЧКУ 2** — custom-branded Counter-Strike 2 launched through SteamEdit.

The launcher is data-driven and can support more СИНОЧКУ games later without rebuilding navigation around each title.

## v3 social platform

v3 adds real, separate СИНОЧКУ accounts and a self-hostable social backend.

### Accounts and profiles

- invite-only account creation
- username, email login and display names
- secure password hashing with ASP.NET Core Identity
- Windows DPAPI-protected desktop sessions
- avatar and profile background uploads
- bio, status text and accent color
- Online / Away / Busy / Invisible / Playing presence
- profile privacy controls
- badges and achievements
- three customizable profile showcases
- profile comments
- tracked game-session playtime and recent activity
- password changes

### Friends, chat and community

- user search
- friend requests and accept/decline/remove
- realtime friend presence
- **Friends & Chat** window
- direct messages
- typing indicators
- unread message counters
- persistent notifications
- game-start notifications
- Community friend-activity feed
- profile navigation from friends/activity
- desktop-style social toasts

### Launcher platform

The existing v2 launcher features remain:

- Steam-style Library / Game / Settings pages
- search, sorting, favorites and installed filters
- Steam and multi-library detection
- SteamEdit discovery + pre-launch hook
- custom Steam cover/hero/logo overrides
- stable/beta update channels
- signed-release-style SHA-256 verification
- updater backup and rollback
- logs and diagnostics
- safe custom game manifests

## Social server

The real social features are provided by `server/SinochkuGames.Server`:

- ASP.NET Core 8
- ASP.NET Core Identity
- JWT auth
- SignalR realtime events
- SQLite persistence
- rate limiting
- invite-only registration
- privacy enforcement
- persistent profile media
- Docker deployment
- Caddy HTTPS reverse proxy

See:

- [v3 social architecture](docs/V3_SOCIAL_PLATFORM.md)
- [account/security notes](docs/ACCOUNT_SECURITY.md)
- [social server deployment](server/README.md)
- [custom game manifests](docs/CUSTOM_GAMES.md)

## Build the launcher

```powershell
dotnet test tests/SinochkuGames.Tests/SinochkuGames.Tests.csproj -c Release

dotnet publish src/SinochkuGames/SinochkuGames.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o dist/v3
```

## Run the social server locally

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:SINOCHKU_BOOTSTRAP_INVITE="dev-invite"
dotnet run --project server/SinochkuGames.Server
```

For real use between different PCs, deploy it behind HTTPS. The repository includes Docker Compose + Caddy configuration.

## Repository layout

- `src/SinochkuGames/` — Windows launcher
- `server/SinochkuGames.Server/` — account/social backend
- `tests/SinochkuGames.Tests/` — launcher tests
- `server/SinochkuGames.Server.Tests/` — social backend tests
- `deploy/` — production Docker/Caddy deployment
- `docs/` — architecture, security and game-manifest docs

## Important security note

СИНОЧКУ accounts are **not Steam accounts**. The launcher must never ask for a Steam password. Chat is stored on the social server and is not end-to-end encrypted, so do not use it for secrets.

## Branding

**СИНОЧКУ GAMES™** uses ™ as an informal brand mark. It does **not** mean the name is a registered trademark.

This project is not affiliated with Valve or Steam.
