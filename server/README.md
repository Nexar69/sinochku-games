# СИНОЧКУ GAMES™ Social Server

The social server powers real accounts, profiles, friends, realtime presence, direct messages, notifications, game sessions, profile showcases and invite-only registration.

## Security model

- Passwords are hashed by ASP.NET Core Identity. The server never stores plaintext passwords.
- The launcher never asks for or stores Steam credentials.
- Desktop sessions use JWT bearer tokens; the Windows client stores its token with DPAPI for the current Windows user.
- Registration is invite-only.
- User-generated custom game manifests cannot execute arbitrary shell commands.
- Avatar/background uploads are limited by size and MIME type.
- Authentication and message endpoints are rate limited.
- Production requires a JWT signing key of at least 32 characters.
- Use HTTPS in production. The included Caddy deployment obtains TLS automatically when a valid domain points to the server.

## Local development

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:SINOCHKU_BOOTSTRAP_INVITE="dev-invite"
dotnet run --project server/SinochkuGames.Server
```

The launcher defaults to `http://localhost:5187`. If dotnet chooses another development port, enter it on the launcher login screen.

## Production with Docker

Copy `deploy/.env.example` to `deploy/.env`, choose a domain, generate a strong random JWT key, and set a private bootstrap invite.

Then:

```bash
cd deploy
docker compose up -d --build
```

Point the launcher's Social Server setting to `https://your-domain.example`.

The first account must use the bootstrap invite and becomes the Founder. The Founder can create more invite codes from the launcher.

## Data

SQLite data lives on the `sinochku-data` Docker volume. Uploaded profile media uses a separate persistent volume.

Back up both volumes before server upgrades.
