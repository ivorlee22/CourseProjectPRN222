# EduPlatform Project Log

Shared handoff log for developers and AI agents. Keep historical entries and add the newest activity entry first.

## Current Status

- Stage: Huy-owned foundation and Course scope implemented.
- Application code: Builds successfully on .NET 10.
- Canonical plan: `implementation_plan.md`.
- Architecture: Strict `Web → BLL → DAL`.
- Test baseline: 10 tests passing, including 1 web integration smoke test.
- Database: Initial migration applied to Neon; pgvector 0.8.1 verified.
- Next milestone: Nguyên implements User/Account authentication flow, then replaces deferred quota integration.

## Completed

- [x] Reviewed and corrected the implementation plan.
- [x] Finalized three-layer MVC architecture and dependency rules.
- [x] Finalized .NET 10, EF Core 10, Neon PostgreSQL, and pgvector.
- [x] Finalized Cookie Authentication and prohibition of Razor Pages.
- [x] Finalized Bootstrap 5, Gmail SMTP, VNPay, and MoMo.
- [x] Added project-specific Codex skill and team agent guidance.
- [x] Integrated the optional personal local taste-skill into the frontend UI/UX workflow.
- [x] Huy tasks 1–8: solution, entities, contracts, EF Core, repositories, DI, layout, Cookie Auth policies.
- [x] Huy tasks 12–13: Course service, MVC controller, and Bootstrap views.
- [x] Huy task 22: Gmail SMTP service through MailKit.
- [x] Huy task 43: responsive Course UI polish and UI-state handling.
- [x] Huy task 47: setup, migration, run, test, and team documentation.
- [x] Huy-scope security review and automated test baseline.

## Outstanding

### Immediate

- [ ] Nguyên task 9: implement `IUserService` and authentication logic.
- [ ] Nguyên task 10: implement AccountController, login/logout/register, and issue Cookie claims.
- [ ] Replace `DeferredCourseQuotaService` with Nguyên's subscription-backed implementation.
- [x] Apply the initial migration to Neon and verify pgvector.

### Implementation

- [ ] Finish Phase 1 User/Account/Admin tasks owned by Nguyên.
- [ ] Phase 2: Documents, RAG chatbot, packages, and subscriptions.
- [ ] Phase 3: VNPay, MoMo, payment processing, and quota enforcement.
- [ ] Phase 4: Reports and full cross-module integration/security testing.

### Credentials Needed Before Integration Testing

- [x] Neon pooled runtime connection string.
- [ ] Neon direct migration connection string.
- [ ] Gemini API key.
- [x] Gmail address and Google App Password.
- [ ] VNPay sandbox credentials and callback URLs.
- [ ] MoMo sandbox credentials and callback URLs.

## Known Debt and Risks

- Full Register → Login → Payment → Course → Document → Chat → Report E2E remains blocked by unimplemented team modules.
- `DeferredCourseQuotaService` intentionally allows creation until Nguyên provides subscription limits.
- Seed accounts are development-only and must be replaced or disabled before production.
- Payment callbacks must be designed for retries and duplicate delivery.
- Quota checks and usage updates must be transactional.
- Document module storage/extraction details remain owned by Nhân and are intentionally not prescribed.
- Taste-skill defaults to React/Next/Tailwind and excludes dashboards; EduPlatform must use its documented MVC/Bootstrap adaptation instead of copying those stack defaults.
- The populated Neon and Gmail secrets are currently in `appsettings.json`; move them to User Secrets or environment variables and leave only placeholders in tracked configuration.

## Activity Log

### 2026-07-02 — Task 10: AccountController + Views

**Owner**

- Nguyên (implemented by Codex).

**Completed**

- Created `EduPlatform.Web/ViewModels/Account/AccountViewModels.cs` — view models for Login, Register, Profile, ChangePassword.
- Created `EduPlatform.Web/Controllers/AccountController.cs` — full auth flow handling cookies, user claims (matching `ClaimsPrincipalExtensions`), login, logout, registration, and profile/password management.
- Updated `EduPlatform.Web/wwwroot/css/site.css` with centered card layouts for `.auth-page`, `.auth-card`, matching the project's visual density and UI rules.
- Created `EduPlatform.Web/Views/Account/Login.cshtml` — login form view.
- Created `EduPlatform.Web/Views/Account/Register.cshtml` — registration form view.
- Created `EduPlatform.Web/Views/Account/Profile.cshtml` — user profile information view without diacritics.
- Created `EduPlatform.Web/Views/Account/ChangePassword.cshtml` — change password form.
- Updated `EduPlatform.Web/Views/Shared/_Layout.cshtml` to add conditional navigation (Dropdown for logged in user, Admin link, Login/Register buttons). 

**Verification**

- `dotnet build` passed 0 errors. Fixed a `CS0019` compatibility issue related to coalescing `RedirectResult?`. 
- `dotnet test` passed 10/10.

**Remaining**

- Task 11: AdminController — User CRUD + Import.

**Blocked**

- None.

### 2026-07-02 — Task 9: UserService

**Owner**

- Nguyên (implemented by Codex).

**Completed**

- Created `EduPlatform.DAL/Repositories/IUserRepository.cs` — interface: GetByNormalizedEmail, GetById, GetAll (paged), Add, Remove, SaveChanges.
- Created `EduPlatform.DAL/Repositories/UserRepository.cs` — EF Core implementation with AsNoTracking paged query.
- Registered `IUserRepository → UserRepository` in `EduPlatform.DAL/DependencyInjection.cs`.
- Created `EduPlatform.BLL/DTOs/Users/UserDtos.cs` — records: UserSummaryDto, LoginCommand, RegisterCommand, CreateUserCommand, UpdateUserRoleCommand, ChangePasswordCommand.
- Created `EduPlatform.BLL/Interfaces/IUserService.cs` — 8 operations: Authenticate, Register, Create (admin), GetById, GetAll, UpdateRole, ChangePassword, SetActive.
- Created `EduPlatform.BLL/Services/UserService.cs` — full implementation with BCrypt auth (same-message for not-found vs wrong-password to prevent user enumeration), role guards, password policy (min 8, upper, lower, digit), email validation, DAL↔BLL enum mapping.
- Registered `IUserService → UserService` in `EduPlatform.BLL/DependencyInjection.cs`.

**Verification**

- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test EduPlatform.sln --no-build`: 10/10 passed.

**Remaining**

- Task 10: AccountController + Views (Login, Logout, Register, Profile).

**Blocked**

- None.

### 2026-07-02 — Prepared safe Git configuration

**Owner**

- Codex.

**Completed**

- Excluded `EduPlatform.Web/appsettings.json` from Git tracking.
- Added sanitized `EduPlatform.Web/appsettings.example.json` for team setup.
- Initialized the Git repository on branch `main` without committing local credentials.
- Kept the personal `taste-skill/` directory locally while excluding it from Git and the initial commit.

**Verification**

- Confirmed the example configuration contains no populated credentials.
- Confirmed the real `appsettings.json` is ignored by Git.
- Scanned all committable files and found no copies of the configured credentials.
- Confirmed no generated build artifacts, logs, local databases, or configured credential values are staged.
- Confirmed `taste-skill/` is ignored and unavailable copies do not block the shared team instructions.

**Remaining**

- Each developer copies `appsettings.example.json` to `appsettings.json` and supplies local credentials.
- Create the remote GitHub repository and push the initial commit.

**Blocked**

- None.

### 2026-07-02 — Applied Neon migration and verified pgvector

**Owner**

- Codex.

**Completed**

- Applied migration `20260701183048_InitialCreate` to the configured Neon database.
- Used a direct Neon endpoint derived transiently from the configured pooled endpoint because `MigrationConnection` is still empty.
- Confirmed the `vector` extension is enabled at version 0.8.1.

**Verification**

- `dotnet ef database update`: passed.
- Confirmed the `Courses` table exists.
- Confirmed `InitialCreate` is recorded in `__EFMigrationsHistory`.
- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test EduPlatform.sln --no-build --no-restore`: 10/10 passed.
- No connection string or Gmail App Password was printed.

**Remaining**

- Populate `ConnectionStrings:MigrationConnection` with the Neon direct endpoint.
- Move the populated database and Gmail secrets out of tracked `appsettings.json`.

**Blocked**

- None.

### 2026-07-02 — Documented Nguyên quota handoff

**Owner**

- Huy scope documented by Codex.

**Completed**

- Added an explicit Nguyên handoff section to `AGENTS.md`.
- Documented that `DeferredCourseQuotaService` is a temporary no-op implementation.
- Added the required replacement algorithm, DI change, removal step, and quota test cases.
- Added code comments at the placeholder implementation and DI registration.

**Verification**

- The handoff points to the existing `ICourseQuotaService` boundary used by `CourseService`.
- No application behavior was changed.
- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.

**Remaining**

- Nguyên must implement and register `SubscriptionCourseQuotaService`.
- Delete `DeferredCourseQuotaService.cs` after replacement.

**Blocked**

- Package and Subscription modules are not implemented yet.

### 2026-07-02 — Split BLL enum files

**Owner**

- Huy scope implemented by Codex.

**Completed**

- Replaced `BLL/Enums/Enums.cs` with `UserRole.cs`, `CourseType.cs`, and `EnrollmentStatus.cs`.
- Preserved the `EduPlatform.BLL.Enums` namespace and existing behavior.

**Verification**

- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test --solution EduPlatform.sln --no-build`: 10/10 passed.

**Remaining**

- None.

**Blocked**

- None.

### 2026-07-02 — Refactored BLL folder structure

**Owner**

- Huy scope implemented by Codex.

**Completed**

- Reorganized BLL into `Interfaces`, `Services`, `DTOs`, `Enums`, `Exceptions`, `Models`, and `Options`.
- Separated `ICourseService`, `ICourseQuotaService`, and `IEmailService` from their implementations.
- Moved Course DTOs and commands into `DTOs/Courses`.
- Updated namespaces, dependency injection, Web imports, tests, agent rules, plan, and README.

**Verification**

- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test --solution EduPlatform.sln --no-build`: 10/10 passed.
- `dotnet format EduPlatform.sln --verify-no-changes --no-restore`: passed.
- Project skill validation passed: `Skill is valid!`.
- Legacy `EduPlatform.BLL.Common`, `.Courses`, and `.Email` namespace references were removed.

**Remaining**

- Apply the same folder convention when other team members add BLL modules.

**Blocked**

- None.

### 2026-07-02 — Implemented Huy-owned tasks

**Owner**

- Huy scope implemented by Codex.

**Completed**

- Created `EduPlatform.sln` with three application projects and one MSTest project.
- Enforced references `Web → BLL → DAL`; Web has no direct DAL reference.
- Added Central Package Management, .NET SDK pinning, and local `dotnet-ef`.
- Added all planned EF Core entities, Fluent API mappings, indexes, seeded demo users, pgvector, and `InitialCreate` migration.
- Added Course repository, DTOs, CourseService, quota contract, CRUD, search, enrollment, invitation, visibility, and authorization rules.
- Added Cookie Authentication, claims/role policies, secure cookie and anti-forgery configuration.
- Added Gmail SMTP service with MailKit, STARTTLS, App Password configuration, and encoded templates.
- Added MVC Course controller and responsive Bootstrap 5 views.
- Added security headers, `.gitignore`, README, and `docs/SECURITY_REVIEW.md`.

**UI/UX**

- Design Read: trustworthy education product UI with clear hierarchy and restrained motion.
- Dials: `DESIGN_VARIANCE 5`, `MOTION_INTENSITY 3`, `VISUAL_DENSITY 5`.
- Verified Home at desktop and 390x844 mobile viewport.
- Confirmed keyboard-oriented skip link, visible focus styles, mobile collapse, no horizontal overflow, reduced-motion handling, and responsive navigation.
- Applied taste-skill contextually; dashboard-only and React/Tailwind guidance was not applied.

**Verification**

- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test --solution EduPlatform.sln --no-build`: 10/10 passed.
- Integration filter: 1/1 passed.
- `dotnet format EduPlatform.sln --verify-no-changes --no-restore`: passed.
- EF Core reports no pending model changes.
- Generated migration SQL contains `CREATE EXTENSION IF NOT EXISTS vector` and `vector(3072)`.
- No Razor Pages or Unit of Work patterns found.
- NuGet vulnerability audit found no vulnerable direct or transitive packages.
- No populated secret fields found in JSON configuration.

**Remaining**

- Huy task 33 is wired through `ICourseQuotaService`; concrete subscription enforcement waits for Nguyên.
- Huy task 45 full E2E waits for User, Payment, Document, Chat, and Report modules.
- Huy task 46 project-wide security sign-off waits for remaining modules; Huy-owned scope is reviewed.
- Neon migration and Gmail delivery require credentials.

**Blocked**

- Neon pooled/direct connection strings.
- Gmail address and Google App Password.
- Other team-owned modules required for full E2E.

### 2026-07-02 - Integrated taste-skill for frontend

**Owner**

- Codex.

**Completed**

- Connected `taste-skill/skills/taste-skill/SKILL.md` to the project-specific skill.
- Added contextual rules for MVC, `.cshtml`, Bootstrap 5, marketing pages, dashboards, forms, and data-heavy screens.
- Added required Design Read, design dials, accessibility checks, and adapted pre-flight logging.

**Verification**

- Read the complete local taste-skill.
- Confirmed its React/Next/Tailwind defaults do not override the fixed EduPlatform stack.
- Reran the official validator successfully: `Skill is valid!`.

**Remaining**

- Apply the frontend workflow when the first MVC UI task begins.

**Blocked**

- None.

### 2026-07-02 — Project governance setup

**Completed**

- Updated `implementation_plan.md` to enforce strict three-layer MVC.
- Removed the Shared project and Unit of Work design.
- Added Neon connection placeholders and integration configuration placeholders.
- Added VNPay and MoMo requirements.
- Created `eduplatform-dotnet-mvc/SKILL.md`.
- Created `AGENTS.md` and this shared project log.

**Verification**

- Confirmed the implementation plan is valid UTF-8.
- Confirmed task ownership totals: Huy 16, Nguyên 11, Nhân 10, Bảo 11.
- Passed the official `skill-creator` validation: `Skill is valid!`.

**Remaining**

- Application implementation has not started.
- Begin Phase 1 tasks 1–8.

## Entry Template

Copy this section to the top of `Activity Log` after material work:

```markdown
### YYYY-MM-DD — Short title

**Owner**

- Name or agent.

**Completed**

- Concrete changes and affected files.

**Verification**

- Commands run and results.

**Remaining**

- Follow-up work and technical debt.

**Blocked**

- Missing decisions, credentials, or external dependencies. Write "None" when clear.
```
