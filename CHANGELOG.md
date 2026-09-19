# Changelog

## v3.0.0-alpha.1 — in development

### Accounts and profiles
- Real invite-only СИНОЧКУ accounts.
- Username/email sign-in with independently hashed passwords.
- Secure Windows session persistence with DPAPI.
- Avatars, profile backgrounds, bio, status and accent color.
- Public / Friends / Private profile visibility.
- Friend request, messaging and activity privacy settings.
- Password changes.
- Profile badges, achievements, showcases and comments.

### Friends and chat
- User search and friend requests.
- Accept, decline and remove friend.
- SignalR realtime presence.
- Online, Away, Busy, Invisible and Playing states.
- Steam-inspired Friends & Chat window.
- Direct messages, typing indicators and unread counts.
- Realtime/persistent notifications.
- Game-start notifications and community activity feed.
- Friend avatars and profile navigation.
- Social toast notifications.

### Game presence
- Supported game process detection updates account presence automatically.
- Server-backed game sessions and playtime.
- Recent activity shared with friends when allowed.
- Game presence returns to Online after exiting.

### Social backend
- ASP.NET Core 8 + Identity + SignalR + SQLite.
- JWT bearer authentication.
- Invite-only registration and Founder invite creation.
- Authentication/message rate limits.
- Profile media upload limits.
- Docker container and Caddy HTTPS deployment.
- Separate persistent database and media volumes.
- Social backend CI/tests.

### Launcher
- Account, Friends & Privacy and Notifications settings.
- Profile, Friends and Community navigation.
- v3 updater channel and release workflow.
- GHCR publishing for the social server.

## v2.0.0-beta.3 — in development

### Platform
- Modular .NET 8 launcher architecture.
- Data-driven game registry.
- Safe LocalAppData custom-game manifests.
- Data-driven runtime process detection.
- Single-instance launcher protection.

### Library
- Steam-style collection grid.
- Search.
- Alphabetical, recently played, and installed-first sorting.
- All / Favorites / Installed filters.
- Favorite game support.
- Per-user Steam custom artwork overrides.

### СИНОЧКУ 2
- Steam AppID 730 auto-detection across all Steam libraries.
- SteamEdit auto-detection and browse fallback.
- SteamEdit autofix launch hook.
- Live running status.
- Tracked launcher playtime.
- Last-played timestamp.

### Settings
- Appearance and accent settings.
- Stable / Beta update channels.
- Steam and SteamEdit diagnostics.
- Registered games page.
- About, logs, diagnostics, and GitHub shortcuts.
- Re-runnable setup check.

### Setup and reliability
- First-run setup wizard.
- Logs and copyable diagnostics.
- GitHub release updater with backup/recovery.
- Release notes in update prompts.
- SHA-256 update verification support.
- One-click rollback to the previous launcher build.
- Automated Windows CI and tests.
