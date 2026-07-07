# Local Docker Compose Setup

The shared `docker-compose.yml` provisions a local **Postgres 17 + pgvector**
container so team members can develop without touching the Neon cluster.

## Why a non-standard port?

The compose file binds host port **5466 → container 5432** because:

- Port 5432 is the well-known Postgres default and is frequently used by
  developers with a system-level Postgres installation.
- 5466 keeps the dev database isolated from any other local stack and lets the
  team run multiple Postgres-based projects side-by-side without conflict.

## Usage

1. (Optional) copy and customise the environment template:
   ```powershell
   Copy-Item .env.example .env
   ```
2. Start the database:
   ```powershell
   docker compose up -d postgres
   ```
3. Verify the container is healthy:
   ```powershell
   docker compose ps
   ```
   `eduplatform-postgres` should show `healthy`.
4. Point your environment at the local instance when running migrations or the
   web project locally:
   ```powershell
   $env:ConnectionStrings__MigrationConnection = "Host=localhost;Port=5466;Database=eduplatform;Username=postgres;Password=postgres;SSL Mode=Disable"
   $env:ConnectionStrings__DefaultConnection = $env:ConnectionStrings__MigrationConnection
   dotnet ef database update --project EduPlatform.DAL --startup-project EduPlatform.Web
   ```
5. Stop and remove the container (data is preserved in the named volume):
   ```powershell
   docker compose down
   ```
6. Remove the container **and** the volume to start from scratch:
   ```powershell
   docker compose down -v
   ```

## Connection string reference

| Setting | Value |
|---------|-------|
| Host | `localhost` (when run from the same machine) |
| Port | `5466` |
| Database | `eduplatform` |
| Username | `postgres` |
| Password | `postgres` (development only) |
| TLS | `SSL Mode=Disable` |

The design-time `AppDbContextFactory` falls back to this connection string when
neither `ConnectionStrings__MigrationConnection` nor
`ConnectionStrings__DefaultConnection` is provided, so the EF tools and the
web app can boot without extra configuration.

## Reset to a clean database

```powershell
docker compose down -v
docker compose up -d postgres
# Re-run your migration command
```

## Switching back to Neon

If you have Neon credentials, prefer the Neon pooled/direct endpoints. The
local container is meant only for environments without network access to the
shared cluster.