# EduPlatform

EduPlatform is an ASP.NET Core MVC application for managing courses, learning documents, contextual chat, subscriptions, and payments.

## Architecture

The application follows strict three-layer architecture:

```text
EduPlatform.Web -> EduPlatform.BLL -> EduPlatform.DAL
```

- `EduPlatform.Web`: MVC controllers, `.cshtml` views, view models, authorization, and Bootstrap 5 UI.
- `EduPlatform.BLL`: services, DTOs, business rules, email, and integration contracts.
- `EduPlatform.DAL`: EF Core entities, `AppDbContext`, migrations, and data repositories.
- `tests/EduPlatform.Tests`: MSTest unit and integration tests. This is not an application layer.

Web does not reference DAL directly. Razor Pages, Unit of Work, and a Shared application project are not used.

BLL is organized by technical responsibility:

```text
EduPlatform.BLL/
├── Interfaces/
├── Services/
├── DTOs/
├── Enums/
├── Exceptions/
├── Models/
└── Options/
```

## Current Huy Scope

Implemented:

- .NET 10 solution and layer references.
- EF Core 10, Npgsql, Neon PostgreSQL configuration, and pgvector migration.
- Core entities and Fluent API mappings.
- Cookie Authentication and role policies.
- Course CRUD, search, enrollment, invitation, visibility, and student list.
- Course quota integration contract.
- Gmail SMTP service through MailKit.
- Bootstrap 5 base layout and Course views.
- MSTest unit tests and web smoke test.

The full account, subscription, document, chat, payment, and report modules belong to other team tasks and are not implemented in this scope.

## Prerequisites

- .NET SDK `10.0.300` or a compatible .NET 10 SDK.
- A Neon PostgreSQL project.
- Neon `vector` extension support.
- Optional Gmail account with two-step verification and an App Password.

Restore local tools and packages:

```powershell
dotnet tool restore
dotnet restore EduPlatform.sln
```

## Configuration

Do not put real secrets in `appsettings.json`.

Use user secrets for local development:

```powershell
dotnet user-secrets --project EduPlatform.Web set "ConnectionStrings:DefaultConnection" "<NEON_POOLED_CONNECTION_STRING>"
dotnet user-secrets --project EduPlatform.Web set "ConnectionStrings:MigrationConnection" "<NEON_DIRECT_CONNECTION_STRING>"
dotnet user-secrets --project EduPlatform.Web set "Email:FromAddress" "<GMAIL_ADDRESS>"
dotnet user-secrets --project EduPlatform.Web set "Email:Username" "<GMAIL_ADDRESS>"
dotnet user-secrets --project EduPlatform.Web set "Email:AppPassword" "<GOOGLE_APP_PASSWORD>"
```

Environment-variable equivalents:

```text
ConnectionStrings__DefaultConnection=
ConnectionStrings__MigrationConnection=
Email__FromAddress=
Email__Username=
Email__AppPassword=
```

Npgsql connection format:

```text
Host=<NEON_HOST>;Port=5432;Database=<DATABASE>;Username=<USERNAME>;Password=<PASSWORD>;SSL Mode=Require;Channel Binding=Require
```

Use Neon pooled connection for runtime and direct connection for migrations.

## Database

The initial migration enables pgvector and creates all planned core tables.

Apply migrations using the direct Neon connection:

```powershell
$env:ConnectionStrings__MigrationConnection="<NEON_DIRECT_CONNECTION_STRING>"
dotnet tool run dotnet-ef database update --project EduPlatform.DAL
```

Create a new migration:

```powershell
dotnet tool run dotnet-ef migrations add <MigrationName> --project EduPlatform.DAL --output-dir Migrations
```

Migrations are never applied automatically during production startup.

## Seed Accounts

The initial migration contains demonstration accounts:

| Role | Email | Development password |
|---|---|---|
| Admin | `admin@eduplatform.local` | `Admin@123` |
| Teacher | `teacher@eduplatform.local` | `Teacher@123` |
| Student | `student@eduplatform.local` | `Student@123` |

These credentials are development-only. Replace or disable seeded users before deployment.

## Run

```powershell
dotnet run --project EduPlatform.Web
```

The home page can start without opening a database connection. Course pages require `DefaultConnection`.

## Build and Test

```powershell
dotnet build EduPlatform.sln
dotnet test --solution EduPlatform.sln
```

Run only integration tests with .NET 10 and Microsoft.Testing.Platform:

```powershell
dotnet test --project tests/EduPlatform.Tests/EduPlatform.Tests.csproj --filter "TestCategory=Integration"
```

## Team Workflow

Before changing code, read:

1. `AGENTS.md`
2. `implementation_plan.md`
3. `PROJECT_LOG.md`
4. `eduplatform-dotnet-mvc/SKILL.md`

For UI work, also read `taste-skill/skills/taste-skill/SKILL.md` when that optional personal local skill is available. It is intentionally excluded from this repository.

After material work, add a dated entry to `PROJECT_LOG.md` with completed work, verification, remaining work, and blockers.
