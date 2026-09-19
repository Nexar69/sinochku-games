# Account and chat security

## Credentials

СИНОЧКУ credentials are independent of Steam. Passwords are hashed by ASP.NET Core Identity. The desktop app receives a JWT and encrypts it with Windows DPAPI for the current Windows account.

Never reuse a Steam password for a СИНОЧКУ account.

## Server transport

Production deployments must use HTTPS. The included Caddy setup handles TLS automatically when the configured domain resolves to the server.

## Registration

Registration is invite-only. The bootstrap invite should be treated like a secret and replaced after the Founder account exists.

## Media uploads

Profile avatars and backgrounds accept PNG, JPEG, or WebP only, with explicit size limits and randomized server-side file names.

## Realtime

SignalR authenticates with the same JWT as the REST API. Typing events are only forwarded between confirmed friends.

## Messages

Direct messages are stored in the social server's SQLite database so chat history and unread state survive reconnects. They are **not end-to-end encrypted**; the server operator can technically access the database. Do not use the chat for secrets.

## Backups

Back up the SQLite data volume and uploads volume. Losing the database loses accounts, friendships, messages and social playtime.
