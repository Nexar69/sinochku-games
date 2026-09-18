# Custom games

СИНОЧКУ GAMES™ v2 can load additional game definitions from:

`%LocalAppData%\SinochkuGames\games`

The built-in library still contains only **СИНОЧКУ 2**. This mechanism is there so future games can be added without rewriting launcher navigation.

## Example

Create `mygame.json`:

```json
{
  "id": "mygame",
  "name": "MY GAME",
  "baseGameName": "Original Steam title",
  "steamAppId": 123456,
  "processName": "mygame",
  "preLaunchHooks": [],
  "heroResourceSuffix": "hero.png",
  "logoResourceSuffix": "logo.png",
  "coverResourceSuffix": "cover.png",
  "description": "A custom game entry."
}
```

## Safety rules

Custom definitions are deliberately declarative:

- IDs may contain letters, numbers, `-`, `_`, and `.`.
- A custom definition cannot replace a built-in game ID.
- Arbitrary shell commands or executable paths are **not** accepted.
- Only launcher-known pre-launch hooks are allowed.
- Currently the only custom hook is `SteamEditAutofix`.
- Steam games are launched through their Steam AppID.

This keeps custom game definitions useful without turning JSON files into arbitrary-code launch scripts.

## Artwork

Built-in games ship with fallback artwork. For Steam-backed games, the launcher can prefer Steam custom artwork when it exists in the user's Steam grid folder.

More flexible per-custom-game artwork support is planned for a later v2 beta.
