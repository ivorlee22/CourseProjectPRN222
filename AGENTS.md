# EduPlatform Agent Guide

This file applies to the entire repository. Every developer or AI agent must read it before making changes.

## Required Reading Order

1. `AGENTS.md` — engineering rules and handoff procedure.
2. `implementation_plan.md` — scope, task ownership, dependencies, and acceptance criteria.
3. `PROJECT_LOG.md` — completed work, pending work, verification, and blockers.
4. Relevant source and test files for the assigned module.

Use the project skill at `eduplatform-dotnet-mvc/SKILL.md` when planning, implementing, reviewing, testing, debugging, or documenting this project.

For frontend, UI, UX, styling, responsive, or visual review work, read `taste-skill/skills/taste-skill/SKILL.md` when that personal local skill is available. Its absence must not block team work; apply the project rules below as the shared fallback.

## Current Technical Decisions

- Architecture: strict 3 Layer with exactly `EduPlatform.Web`, `EduPlatform.BLL`, and `EduPlatform.DAL`.
- Dependency direction: `Web → BLL → DAL`.
- Runtime: .NET 10.
- Web framework: ASP.NET Core MVC controllers and `.cshtml` views.
- Razor Pages are prohibited: do not use `@page`, `PageModel`, `AddRazorPages`, or `MapRazorPages`.
- Authentication: Cookie Authentication with claims and role policies.
- Database: EF Core 10 + Npgsql + Neon PostgreSQL + pgvector.
- UI: Bootstrap 5.
- UI quality guide: project frontend rules, optionally supplemented by the ignored personal local `taste-skill`.
- Email: Gmail SMTP through MailKit and Google App Password.
- Payments: VNPay only (MoMo đã bị loại bỏ khỏi luồng thanh toán, PaymentMethod enum giữ lại để tương thích dữ liệu cũ).
- Unit of Work is not used.

## Layer Boundaries

### EduPlatform.Web

- MVC controllers, `.cshtml` views, view models, authorization filters, SignalR hubs, static assets, and `Program.cs`.
- May reference BLL only.
- Controllers call BLL services; they never call DAL, repositories, or `AppDbContext`.

### EduPlatform.BLL

- Service interfaces and implementations, DTOs, business rules, validation, quota enforcement, and integration orchestration.
- May reference DAL.
- Must map DAL entities to DTOs before returning data to Web.
- Organize BLL by technical responsibility:
  - `Interfaces/` for service contracts.
  - `Services/` for service implementations.
  - `DTOs/<Module>/` for commands and response DTOs.
  - `Enums/`, `Exceptions/`, `Models/`, and `Options/` for their respective cross-cutting types.
- Do not place interfaces and implementations together in feature folders.

### EduPlatform.DAL

- EF Core entities, `AppDbContext`, Fluent API mappings, migrations, and repositories for concrete query needs.
- Must not reference BLL or Web.
- Must not expose `IQueryable` outside DAL.

Use `AppDbContext` transactions directly. Do not introduce Unit of Work or a generic repository merely to wrap every `DbSet`.

## Security and Integration Rules

- Never commit real connection strings, passwords, API keys, App Passwords, gateway secrets, or production callback URLs.
- Use environment variables or user secrets for local development.
- Use the Neon pooled endpoint for runtime and direct endpoint for migrations.
- Do not run migrations automatically during production startup.
- Validate anti-forgery tokens on state-changing browser actions.
- Verify VNPay and MoMo signatures and process callbacks idempotently.
- Never activate a subscription twice for the same payment.
- Email failure must not invalidate an already confirmed payment; record and retry it separately.

## Frontend and Taste-Skill Rules

- Before UI implementation, provide a one-line Design Read and select the three taste-skill dials: design variance, motion intensity, and visual density.
- Project constraints override taste-skill stack defaults. Keep MVC, `.cshtml`, Bootstrap 5, and native JavaScript.
- Do not add React, Next.js, Tailwind, shadcn/ui, or a second design system unless the user explicitly changes the architecture.
- Use the full taste-skill landing-page process for public and marketing-oriented pages such as Home and Package/Pricing.
- Taste-skill is not intended for dashboards, data tables, or complex product workflows. For Admin, reports, chat, and role dashboards, apply only its relevant hierarchy, consistency, accessibility, responsive, copy, motion, and UI-state rules.
- Preserve routes, navigation labels, form field names, legal copy, analytics hooks, and brand assets unless explicitly authorized.
- Every interactive screen must handle loading, empty, validation, error, success, disabled, hover, focus, and active states as applicable.
- Check keyboard navigation, visible focus, mobile layout, reduced motion, and WCAG AA contrast.
- Do not use em-dash or en-dash characters in user-visible UI copy.
- Record the Design Read, dial values, and applicable pre-flight result in `PROJECT_LOG.md`.

## Team Workflow

Before coding:

- Check `PROJECT_LOG.md` for active work and blockers.
- Confirm the task owner and dependencies in `implementation_plan.md`.
- Inspect existing changes before editing shared files.
- For UI work, read the local taste-skill when available and identify whether the screen is marketing-oriented or product/data-oriented.

While coding:

- Keep changes inside the assigned module unless a cross-module contract change is necessary.
- Coordinate changes to shared contracts, `Program.cs`, `AppDbContext`, entities, and migrations.
- Add or update tests for business rules and failure paths.

Before handoff:

- Build affected projects and run relevant tests.
- Record commands and results in `PROJECT_LOG.md`.
- Record unfinished work, missing credentials, migration needs, and technical debt.
- Never remove previous log entries; add a new dated entry at the top of the activity log.

## Nguyên Handoff: Course Quota Integration

`DeferredCourseQuotaService` is a temporary no-op placeholder. It allows every course creation request and must not remain as the production quota implementation.

When implementing Package and Subscription tasks, Nguyên or the assigned agent must:

1. Create `SubscriptionCourseQuotaService` under `EduPlatform.BLL/Services/`.
2. Implement the existing `EduPlatform.BLL.Interfaces.ICourseQuotaService` contract.
3. Resolve the user's active subscription and associated package.
4. Read `Package.MaxCourses`.
5. Compare that limit with `currentCourseCount` supplied by `CourseService`.
6. Throw `CourseQuotaExceededException` when the limit is reached.
7. Decide and document the fallback when no active subscription exists. The expected default is the Free package limit.
8. Replace this DI registration:

```csharp
services.AddScoped<ICourseQuotaService, DeferredCourseQuotaService>();
```

with:

```csharp
services.AddScoped<ICourseQuotaService, SubscriptionCourseQuotaService>();
```

9. Delete `DeferredCourseQuotaService.cs` after the real implementation is registered.
10. Add tests for active subscription, expired subscription, missing subscription, under-limit, exact-limit, Admin bypass, and concurrent course creation.

Do not modify `CourseService` to query subscription tables directly. The quota integration must remain behind `ICourseQuotaService`.

The quota handoff is complete only when no production registration references `DeferredCourseQuotaService` and Course creation rejects over-limit requests.

## Definition of Done

A task is done only when:

- Its acceptance criteria are met.
- Layer boundaries remain valid.
- Relevant build and tests pass, or skipped verification is explicitly documented.
- Secrets are not committed.
- `PROJECT_LOG.md` is updated.
