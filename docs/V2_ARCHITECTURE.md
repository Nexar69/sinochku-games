# СИНОЧКУ GAMES™ v2 architecture

v2 changes the launcher from a single hard-coded CS2 window into a small data-driven game platform.

## Layers

- Models — game definitions, runtime state, and user settings.
- Services — Steam discovery, game detection, launching, settings persistence, logs, assets, and updates.
- UI — shell plus independent Library, Game, and Settings pages.
- Games — embedded JSON definitions. Adding a supported game should normally mean adding one definition and artwork rather than rebuilding navigation logic.
- Tests — parsing and version/update primitives.
- GitHub Actions — build/test artifacts on changes and tagged v2 release publishing.

## Game registry

Every game definition declares a stable ID, display name, base game, Steam AppID, runtime process name, pre-launch hooks, artwork resource names, and description. Built-in definitions are embedded in the executable. Additional safe JSON definitions can be dropped into `%LocalAppData%\\SinochkuGames\\games`; built-in IDs cannot be overridden and only whitelisted launch hooks are accepted.

СИНОЧКУ 2 currently uses the SteamEditAutofix pre-launch hook, Steam AppID 730, and process name `cs2`. Runtime detection is data-driven rather than hard-coded to CS2.

## Local data

User-specific state lives under %LocalAppData%\SinochkuGames.

This includes settings, logs, tracked playtime, and updater staging files. Repository assets and game definitions remain read-only inside the app. When Steam custom artwork exists for the selected app, the launcher prefers it over embedded fallback artwork.

## Update channels

- Stable ignores GitHub prereleases.
- Beta also accepts prereleases.

The v2 updater only considers releases whose tags begin with v2. and looks for a SinochkuGames.exe release asset.

## Trademark notation

The launcher uses СИНОЧКУ GAMES™ as an informal brand mark. ™ does not claim registration and should not be replaced with ® without an actual registration.
