---
name: eduplatform-dotnet-mvc
description: Enforce EduPlatform's project-specific architecture, delivery workflow, and contextual frontend design standards. Use when planning, implementing, reviewing, testing, debugging, or documenting code in the EduPlatform repository, especially ASP.NET Core MVC, Bootstrap UI/UX, Cookie Authentication, EF Core/Neon PostgreSQL, pgvector RAG, Gmail, VNPay, or MoMo work.
---

# EduPlatform .NET MVC

Apply the repository's architectural decisions consistently and leave enough project state for the next team member to continue safely.

## Start With Project Context

Locate the repository root containing `implementation_plan.md`, then read these files completely:

1. `AGENTS.md`
2. `implementation_plan.md`
3. `PROJECT_LOG.md`

Treat the latest user instruction as highest priority. Use `AGENTS.md` for engineering rules, `implementation_plan.md` for scope and ownership, and `PROJECT_LOG.md` for current status.

Inspect the existing tree before changing files. Preserve unrelated work and respect module ownership.

For any frontend, UI, UX, styling, responsive, or visual review task, also read `taste-skill/skills/taste-skill/SKILL.md` completely when that optional personal local skill is available. Continue with the shared rules in this file when it is absent.

## Follow the Delivery Workflow

1. Identify the requested feature, owning module, dependencies, and acceptance criteria.
2. Place every new type in the correct layer before implementing behavior.
3. Make the smallest coherent change that completes the request.
4. Validate in proportion to the change:
   - Restore and build affected projects.
   - Run relevant automated tests.
   - Check project references when layer boundaries change.
   - Test failure paths for authentication, payment, email, and external APIs.
5. Update `PROJECT_LOG.md` after every material change. Record completed work, verification, remaining work, and blockers without deleting prior entries.

Do not mark work complete when build or required tests were not run; record the reason and remaining verification explicitly.

## Enforce Three Layers

Use exactly these application projects:

```text
EduPlatform.Web -> EduPlatform.BLL -> EduPlatform.DAL
```

- Put MVC controllers, `.cshtml` views, view models, filters, hubs, static assets, and the composition root in `Web`.
- Put business services, service interfaces, DTOs, business validation, quotas, and external integration orchestration in `BLL`.
- Organize BLL into `Interfaces/`, `Services/`, `DTOs/<Module>/`, `Enums/`, `Exceptions/`, `Models/`, and `Options/`; keep contracts separate from implementations.
- Put EF Core entities, `AppDbContext`, mappings, migrations, and data repositories in `DAL`.
- Never add a direct `Web` reference to `DAL`.
- Never expose EF Core entities from BLL to Web; map them to DTOs.
- Never access `AppDbContext` or repositories from a controller.
- Do not create a `Shared` application project.
- Do not use Unit of Work. Use `AppDbContext` transactions and add only repositories justified by concrete queries.
- Do not return `IQueryable` outside DAL.

## Enforce MVC and Authentication

- Use ASP.NET Core MVC controllers and `.cshtml` views.
- Do not use Razor Pages: no `@page`, `PageModel`, `AddRazorPages`, or `MapRazorPages`.
- Treat Razor views as the MVC view engine; they are allowed and expected.
- Use Cookie Authentication with claims and role-based `[Authorize]` policies.
- Hash passwords with BCrypt and never store or log plaintext credentials.
- Use anti-forgery validation for state-changing browser requests.
- Use Bootstrap 5 for the UI.

## Apply Taste Skill to Frontend Work

When available, use `taste-skill/skills/taste-skill/SKILL.md` as an optional UI/UX quality guide, subject to this project's fixed architecture and stack.

Before implementing a UI change:

1. State the one-line Design Read required by taste-skill.
2. Select and record `DESIGN_VARIANCE`, `MOTION_INTENSITY`, and `VISUAL_DENSITY`.
3. Inspect existing brand tokens, layouts, routes, form names, accessibility behavior, and Bootstrap conventions before redesigning.
4. Apply the relevant taste-skill pre-flight checks before handoff.

Adapt taste-skill to EduPlatform as follows:

- Keep ASP.NET Core MVC, `.cshtml`, Bootstrap 5, and native JavaScript. Do not introduce React, Next.js, Tailwind, shadcn/ui, or another design system from taste-skill unless the user explicitly changes the project stack.
- Treat Bootstrap 5 as the single design system. Customize its variables and project CSS instead of shipping an untouched template look.
- Apply the full landing-page guidance to public or marketing-oriented surfaces such as Home, About, Package/Pricing, Login, and Register when appropriate.
- Do not force landing-page patterns onto Admin, Teacher, Student, report, chat, or data-heavy screens. Taste-skill explicitly excludes dashboards, data tables, and multi-step product UI.
- For product screens, still apply its cross-cutting rules: clear hierarchy, consistent palette and radii, responsive collapse, accessible contrast and focus, motivated motion, reduced-motion fallback, concise copy, and complete loading/empty/error/success states.
- Preserve route slugs, navigation labels, form field names, legal copy, analytics hooks, and existing brand assets unless the user explicitly authorizes a change.
- Do not invent statistics, testimonials, course data, user data, or payment results for production UI.
- Avoid frontend AI tells identified by taste-skill, including generic three-card repetition, decorative status dots, fake screenshots, duplicate CTA intent, excessive pills, inconsistent themes, and inaccessible contrast.
- Use no em-dash or en-dash characters in user-visible interface copy, as required by taste-skill.
- Use animation only when it communicates hierarchy, feedback, storytelling, or state transition. Prefer CSS and small native JavaScript enhancements compatible with MVC.

When a taste-skill instruction conflicts with EduPlatform's architecture, Bootstrap 5, MVC, accessibility, security, or explicit user requirements, follow EduPlatform's rule and document the adaptation in `PROJECT_LOG.md`.

## Handle Data and Integrations

- Target .NET 10 and EF Core 10 with the matching Npgsql EF provider.
- Connect to Neon PostgreSQL through Npgsql/EF Core; do not introduce Neon Data API.
- Use the pooled Neon endpoint at runtime and the direct endpoint for migrations.
- Require TLS and enable `pgvector` in the initial migration.
- Do not auto-migrate during production startup.
- Keep connection strings, Gmail App Password, Gemini key, and payment secrets out of source control.
- Send Gmail through MailKit with SMTP port 587 and STARTTLS.
- Support both VNPay and MoMo behind payment abstractions.
- Verify gateway signatures and make payment callbacks idempotent.
- Update payment state and subscription activation atomically.
- Let the owner of the Document module choose its internal extraction, chunking, and storage details while preserving layer boundaries.

## Finish Cleanly

Before handing off:

- Ensure the change satisfies the relevant checklist in `implementation_plan.md`.
- For UI work, record the Design Read, dial values, responsive checks, and the taste-skill pre-flight result when that local skill is available.
- State exactly what was built and tested.
- Add unresolved decisions, missing credentials, failed checks, and technical debt to `PROJECT_LOG.md`.
- Do not claim the whole task is complete if follow-up work remains.
