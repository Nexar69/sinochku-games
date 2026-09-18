# СИНОЧКУ GAMES™

A custom Steam-style Windows game launcher made for the СИНОЧКУ meme universe.

## Current games

- **СИНОЧКУ 2** — launches Counter-Strike 2 through SteamEdit with the custom local branding.

## Features

- Steam-style library home with game cards
- Per-game detail page with custom hero/logo/cover artwork
- Automatic Steam and Steam-library detection
- Automatic CS2 (AppID 730) detection
- Automatic SteamEdit discovery with one-time browse fallback
- Settings page reserved for future options
- Built-in GitHub Releases update checker and self-updater
- Portable Windows executable; no admin rights required for normal use

## Updating

The launcher checks the latest GitHub Release in the background. When a newer release exists, an **UPDATE** button appears in the title bar. The launcher downloads the new executable, replaces itself after closing, and restarts automatically.

## Building

Run `build.ps1` on Windows. The project intentionally uses the .NET Framework compiler that ships with Windows on this development setup, so no NuGet packages are needed.

## Branding note

**СИНОЧКУ GAMES™** uses the ™ symbol as an informal brand mark. It does **not** mean the name is a registered trademark.

This is a fan/custom launcher and is not affiliated with Valve or Steam.
