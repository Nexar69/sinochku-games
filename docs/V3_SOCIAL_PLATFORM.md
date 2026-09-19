# СИНОЧКУ GAMES™ v3 social platform

v3 turns the launcher into a small account-based gaming platform while preserving the existing game launcher.

## Accounts

Accounts are separate from Steam. The server uses ASP.NET Core Identity for password hashing and JWT bearer sessions. The Windows launcher protects its saved token with Windows DPAPI.

Registration is invite-only. A production server starts with a private bootstrap invite supplied through an environment variable. The first account becomes Founder and can create additional invite codes.

## Profiles

Profiles support:

- username and display name
- avatar and profile background
- bio and status text
- accent color
- Online / Away / Busy / Invisible / Playing presence
- tracked game-session playtime
- last-seen time
- badges
- achievements
- up to three showcase slots
- profile comments
- recent game activity

## Friends and chat

The social server provides:

- user search
- friend requests, accept/decline, remove friend
- realtime online/game presence
- friend activity
- private direct messages
- typing indicators
- unread message counts
- realtime notifications
- persistent notifications
- Steam-inspired Friends & Chat window

SignalR is used for live events; messages and friend state are also persisted in SQLite so reconnecting does not lose history.

## Presence and games

The launcher watches its data-driven game registry. When a supported game process starts:

1. the launcher creates a social game session,
2. presence becomes Playing,
3. friends receive the new presence,
4. friends receive a game-start notification,
5. session time is tracked server-side.

When the process closes, the session ends and presence returns to Online.

## Privacy

Per-account controls include:

- Public / Friends / Private profile visibility
- allow/deny friend requests
- allow/deny messages from friends
- show/hide game activity

Invisible presence is sent to friends as Offline.

## Hosting

The backend is an ASP.NET Core 8 + SignalR service using SQLite. The repository includes:

- a production Dockerfile
- Docker Compose
- persistent volumes for database and uploads
- Caddy reverse proxy with automatic HTTPS
- GitHub CI for server and launcher
- v3 release automation
- GHCR container publishing

See `server/README.md` and `deploy/.env.example`.

## Deliberate non-features

- Steam passwords are never requested or stored.
- Chat is private to the server database but is not end-to-end encrypted.
- Join Game / Invite to Game requires a reliable game-specific lobby/session identifier. v3 does not fake this for CS2; those actions will be added only when the launcher can obtain a valid join target.
- Email password recovery requires an outbound mail provider and is not enabled until one is configured.
