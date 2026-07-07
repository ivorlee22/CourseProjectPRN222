# EduPlatform Project Log

Shared handoff log for developers and AI agents. Keep historical entries and add the newest activity entry first.

## Current Status

- Stage: Huy-owned foundation and Course scope implemented.
- Application code: Builds successfully on .NET 10.
- Canonical plan: `implementation_plan.md`.
- Architecture: Strict `Web → BLL → DAL`.
- Test baseline: 50 tests passing, 1 opt-in live Gemini test skipped without credentials.
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

### 2026-07-08 - Chat markdown rendering

**Owner**

- Bảo chat scope (implemented by Codex).

**Completed**

- Added safe markdown rendering for chat assistant and user messages in the Chat MVC view.
- Supported common Gemini output formatting for bold text, inline code, unordered lists, ordered lists, paragraphs, and line breaks.
- Updated SignalR streaming chat rendering so markdown formatting appears during live responses instead of only after reload.
- Escaped HTML before applying the small markdown whitelist to avoid rendering unsafe HTML from chat content.
- Added renderer unit tests for bold markdown, bullet lists, and HTML escaping.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 71 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.
- `node --check .\EduPlatform.Web\wwwroot\js\chat.js` passed.

### 2026-07-08 - Task 34 chat limit integration

**Owner**

- Bảo chat scope (implemented by Codex).

**Completed**

- Integrated `IChatQuotaService.EnsureCanSendMessageAsync` into both normal chat send and SignalR streaming flows.
- Added a pre-retrieval quota check so exhausted users do not spend embedding, retrieval, or Gemini calls when they are already over the daily chat limit.
- Rechecked quota inside the same chat persistence transaction before saving the user message, assistant message, and retrieval logs.
- Added a chat repository transaction wrapper so the Task 32 `FOR UPDATE` user lock runs on the same scoped `AppDbContext` used to save chat data.
- Handled `ChatQuotaExceededException` as a user-facing error in MVC fallback posts and SignalR streams.
- Added client-side handling for SignalR `error` events so quota exhaustion is shown as a friendly assistant notice and the question stays in the composer.
- Added unit coverage for quota exhaustion before Gemini and quota exhaustion during persistence.
- Fixed daily quota start-of-day calculation to always pass UTC `DateTimeOffset` values to PostgreSQL/Npgsql.
- Added unit coverage for timezone-offset servers so the quota query does not pass `+07:00` offsets into `timestamp with time zone`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 68 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Notes**

- No migrations or database shape changes were needed for Task 34.
- Live UI quota prompt still needs manual browser verification with a package/user whose `DailyChats` limit is exhausted.

### 2026-07-08 - Chat message order and workspace polish

**Owner**

- Bảo chat scope (implemented by Codex).

**Completed**

- Fixed chat message ordering when user and assistant messages share the same timestamp by sorting user messages before assistant messages.
- Widened the Chat workspace beyond the default Bootstrap container for a less cramped desktop layout.
- Rebalanced desktop Chat columns so the conversation area gets more width while the history/source panels stay useful but less dominant.
- Reduced Chat page header and empty-state visual pressure, then gave the conversation/composer wider readable measure.
- Disabled browser scroll restoration on the Chat page and avoided auto-scrolling empty sessions so the welcome state is not clipped.

**Verification**

- `node --check EduPlatform.Web/wwwroot/js/chat.js`: passed.
- `dotnet build EduPlatform.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test EduPlatform.sln -c Release --no-build --no-restore`: passed, 65 succeeded and 1 opt-in live Gemini test skipped without `GEMINI_API_KEY`.
- Debug build was blocked by the currently running Visual Studio/EduPlatform.Web process locking Debug DLLs, so Release build was used for compile verification.

**Remaining**

- Manual browser check on the authenticated Chat page is recommended after restarting the app.

**Blocked**

- None.

### 2026-07-07 — UI/UX Redesign and Role Authorization Fixes

**Owner**

- Codex (Agent).

**Completed**

- Fixed business logic regarding course creation and package purchases: Course creation is Admin-only; Package purchasing is Student-only.
- Updated `_Layout.cshtml`: Restricted "Gói cước" to Students/Unauthenticated, and "Lịch sử thanh toán" to Students.
- Updated `Home/Index.cshtml`: Restricted "Tạo khóa học" to Admin, and "Nâng cấp tài khoản" to Students/Unauthenticated.
- Redesigned `Packages.cshtml` with a premium UI (glassmorphism, gradients, hover effects, dedicated MoMo/VNPay styling).
- Added `[Authorize(Roles = "Student")]` to `PaymentController.CreatePayment`.
- Added `UserId` field to `Profile.cshtml` with a copy-to-clipboard button so Students can easily share their ID for manual enrollment.

**Verification**

- `dotnet build EduPlatform.sln`: passed with 0 warnings and 0 errors.
- Visual inspection of the updated Views.

**Remaining**

- Perform end-to-end sandbox testing for MoMo and VNPay once keys are populated.

**Blocked**

- MoMo sandbox credentials and callback URLs.
- VNPay sandbox credentials.

### 2026-07-07 — Paused VNPay and Initiated MoMo Integration

**Owner**

- Nhân.

**Completed**

- Verified mathematically that `VNPayService.cs` produces exact URLs and HMAC-SHA512 hashes as the standard VNPay Java SDK.
- Suspended VNPay debugging as the "Sai chữ ký" error stems from external configuration issues (e.g. invalid `HashSecret` or VNPay backend misconfiguration of the sandbox account algorithm).
- Refactored `MoMoService.cs` to execute asynchronous HTTP POST calls to MoMo Sandbox endpoint.
- Updated `IPaymentService` and `PaymentService.cs` to await `MoMoService.CreatePaymentUrlAsync`.
- Added `AddHttpClient()` to `DependencyInjection.cs` to support MoMo HTTP operations.

**Verification**

- `dotnet build EduPlatform.sln`: passed with 0 warnings and 0 errors.

**Remaining**

- Obtain functioning Sandbox MoMo credentials (`PartnerCode`, `AccessKey`, `SecretKey`).
- Perform end-to-end sandbox testing for MoMo payment generation and IPN/Return callback validation.

**Blocked**

- MoMo sandbox credentials and callback URLs.
- VNPay sandbox credentials verification (suspended).

### 2026-07-07 — Fixed VNPay Signature Mismatch

**Owner**

- Nhân.

**Completed**

- Fixed `VNPayService.cs` URL encoding by changing `HttpUtility.UrlEncode` to `System.Net.WebUtility.UrlEncode` to correctly encode space to `+` and generate standard uppercase hex values (`%2F` instead of `%2f`).
- Fixed `VNPayService.cs` HMAC-SHA512 hash generation by adding `.ToLowerInvariant()` to `Convert.ToHexString(hashValue)` as VNPay requires the signature hash to be lowercase.

**Verification**

- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.

**Remaining**

- Need real sandbox credentials to test VNPay integration fully from end-to-end.

**Blocked**

- VNPay sandbox credentials and callback URLs.
### 2026-07-07 - Task 42: UI/UX Polish (User, Package, Subscription)

**Owner**

- Nguyên (implemented by Codex).

**Completed**

- Replaced TempData inline alerts with Bootstrap Toasts in `_Layout.cshtml` and initialized them in `site.js`.
- Added form loading state handling in `site.js` (disable button, show spinner, and prevent double submission).
- Refactored `.field-validation-error` CSS to include a warning icon and softer red text color.
- Ensured Account views (`Login`, `Register`, `Profile`, `ChangePassword`) are fully responsive on mobile devices using `narrow-content` and card layouts.
- Verified responsiveness of pricing grid and subscription tables on mobile.

**Verification**

- `dotnet build` passed 0 warnings and 0 errors.
- Checked responsiveness visually.

**Remaining**

- None for Task 42.

**Blocked**

- None.

### 2026-07-07 - Task 32: Chat Quota Subscription Enforcement

**Owner**

- Nguyên (implemented by Codex).

**Completed**

- Enforced `DailyChats` limit without modifying `Entities`.
- Created `ChatQuotaExceededException`.
- Created `IChatQuotaRepository` and `ChatQuotaRepository` with `FOR UPDATE` lock to prevent race conditions during message counting.
- Created `SubscriptionChatQuotaService` to handle limits, admin bypass, and Free package fallback.
- Wrote MSTest unit tests `SubscriptionChatQuotaServiceTests` testing 4 core scenarios.

**Verification**

- `dotnet build` passed 0 warnings and 0 errors.
- `dotnet test` passed (66/66 tests passed).

**Remaining**

- Team Chat (Bảo - Task 34) must call `EnsureCanSendMessageAsync` within their database transaction when saving new messages.

**Blocked**

- Task 34 (Chat API) must be completed to test end-to-end quota enforcement.

### 2026-07-07 - Subscription management page

**Owner**

- Nguyen subscription scope (implemented by Codex).

**Completed**

- Added MVC `SubscriptionController` for Student subscription management.
- Added Subscription Web view models and `Views/Subscription/Index.cshtml`.
- Displayed the current active subscription, quota details, subscription dates, and full subscription history.
- Added Student-only `Gói của tôi` navigation while keeping Teacher and Admin out of this personal purchase-management flow.
- Added Renew and Cancel post actions with anti-forgery validation. Renew creates a pending subscription request for the same package until payment gateways are implemented; Cancel delegates to `ISubscriptionService.CancelSubscriptionAsync`.
- Added scoped responsive subscription-management styles in `site.css`.
- Added controller tests for current subscription display, history-only display, Renew, and Cancel.

**UI/UX**

- Design Read: Student subscription management page with clear current-plan status, dense history table, and restrained action controls.
- Dials: `DESIGN_VARIANCE 4`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 7`.
- `fk` skill context script reported `NO_PRODUCT_MD`, but its referenced `init.md` was not present in the installed skill; the EduPlatform MVC/Bootstrap rules and `fk` product-register guidance were applied as fallback.
- Covered empty current-subscription state, empty history state, active/pending/cancelled/expired badges, disabled-free layout constraints through responsive collapse, focus/hover inherited from Bootstrap buttons, and mobile table scrolling.

**Verification**

- `dotnet build EduPlatform.sln -c Release --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test EduPlatform.sln -c Release --no-build --no-restore` - passed: 61 succeeded, 0 failed, 1 skipped opt-in live Gemini test.
- Debug build was not used for final verification because local `EduPlatform.Web` and Visual Studio processes were locking Debug output DLLs; the initial sandboxed build also could not read the user NuGet configuration.
- Checked new Subscription UI files for en dash and em dash characters.

**Remaining**

- Real Renew payment flow waits for VNPay/MoMo payment integration.
- Admin global subscription management remains Task 31 and is intentionally separate from this Student page.

**Blocked**

- VNPay/MoMo payment integration is required for real checkout and renewal payment.

### 2026-07-07 - Package price table page

**Owner**

- Nguyen package scope (implemented by Codex).

**Completed**

- Added MVC `PackageController` with public Pricing page and authenticated `Buy` post placeholder.
- Added Package pricing view models under Web.
- Added `Views/Package/Index.cshtml` to display Free, Plus, Pro, and Max packages, compare MaxCourses, DailyChats, DurationDays, and show the current active subscription.
- Added the `Bảng giá` navigation link for anonymous users, Students, and Admins while keeping Teachers out of the package purchase surface.
- Added responsive pricing styles in `site.css`.
- Added controller tests for anonymous package display, current-package highlight, Admin pricing access, and safe Buy redirect behavior.

**UI/UX**

- Design Read: public price table page with clear package comparison, current-plan emphasis, and direct purchase intent for Students.
- Dials: `DESIGN_VARIANCE 6`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 6`.
- Taste-skill local file was not available, so the shared EduPlatform MVC/Bootstrap frontend rules were applied.
- Covered anonymous, Student, Teacher, and Admin states, including Student purchase access, Admin pricing access without purchase, Teacher exclusion from pricing navigation, current-package disabled state, hover/focus inherited from Bootstrap buttons, responsive 4/2/1-column pricing grid, and mobile table scrolling.

**Verification**

- `dotnet build EduPlatform.sln --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test EduPlatform.sln --no-build --no-restore` - passed: 57 succeeded, 0 failed, 1 skipped opt-in live Gemini test.
- Checked new Package UI files for en dash and em dash characters.
- Local HTTPS run is blocked by missing or untrusted ASP.NET dev certificate; HTTP-only run starts in foreground, but the tool cannot keep it running in the background because Windows `Start-Process` fails on duplicate `Path`/`PATH` environment entries.

**Remaining**

- Payment gateways are not implemented yet, so `Mua ngay` validates the package and returns a clear pending-payment message instead of creating a real payment.

**Blocked**

- VNPay/MoMo payment integration is required for real checkout.

### 2026-07-07 - Course quota subscription integration

**Owner**

- Nguyen handoff (implemented by Codex).

**Correction**

- Clarified role model: Admin creates courses and assigns teachers; Teacher sees assigned courses and uploads documents; Student sees public courses and enrolls. Course creation remains Admin-only.

**Completed**

- Added `SubscriptionCourseQuotaService` and registered it for `ICourseQuotaService`.
- Deleted the temporary `DeferredCourseQuotaService`.
- Added `IPackageRepository.GetFreePackageAsync` for Free-package fallback without hard-coding a package id.
- Updated `CourseService` so Teacher course creation checks quota before persistence while Admin creation bypasses quota.
- Added quota service tests for active subscription, exact-limit, over-limit, expired subscription fallback, missing subscription fallback, Free package interactions, and concurrent exact-limit requests.
- Added CourseService regression tests proving quota failure does not add or save a course.

**Verification**

- `dotnet restore EduPlatform.sln` - passed after allowing access to the user NuGet configuration.
- `dotnet build EduPlatform.sln --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test EduPlatform.sln --no-build --no-restore` - passed: 50 succeeded, 0 failed, 1 skipped opt-in live Gemini test.
- `rg -n "DeferredCourseQuotaService" EduPlatform.BLL EduPlatform.DAL tests` - no source or test references remain.

**Remaining**

- Web Course Create is still governed by the existing MVC policy and UI flow; this handoff only changed the BLL quota boundary and service tests.

**Blocked**

- None.

### 2026-07-06 — Chat business-rule hardening after Tasks 19–21

**Owner**

- Bảo (implemented by Codex).

**Completed**

- Removed the fabricated top-result citation fallback; retrieval logs and source cards now represent only citation ranks explicitly present in Gemini's answer.
- Added up to 10 recent messages, capped at 6,000 characters and biased toward the newest context, to support follow-up questions without treating chat history as a factual source.
- Limited Gemini output to 2,048 tokens with a 256-token thinking budget for the configured Gemini 2.5 Flash model.
- Rejects `MAX_TOKENS` responses so incomplete answers are not persisted as successful chat messages.
- Removed client-supplied `courseId` from send/delete forms and resolves the canonical course through the owned session before redirecting.

**Verification**

- `dotnet build EduPlatform.sln -c Release --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test tests/EduPlatform.Tests/EduPlatform.Tests.csproj -c Release --no-build --filter "TestCategory!=LiveGemini"` — 39 tests passed.
- Live Gemini streaming smoke test — 1 test passed with a complete answer and one persisted citation.
- No database migration is required.

### 2026-07-06 — Task 21: SignalR chat streaming

**Owner**

- Bảo (implemented by Codex).

**Completed**

- Added an authorized `ChatHub` at `/hubs/chat` with server-to-client streaming through `IAsyncEnumerable`.
- Required the MVC anti-forgery token for every state-changing SignalR `SendMessage` stream invocation in addition to Cookie Authentication.
- Added Gemini `streamGenerateContent` SSE handling and streamed RAG orchestration in BLL without crossing the three-layer boundary.
- Persisted the user message, complete assistant answer, and cited retrieval logs only after a successful stream; interrupted or failed streams do not save partial messages.
- Added the self-hosted Microsoft SignalR 10.0.0 browser client to remain compatible with the existing CSP and avoid a runtime CDN dependency.
- Updated the native chat client to render response deltas live, reconnect automatically, prevent duplicate sends per composer, and retain the existing anti-forgery-protected MVC POST as a fallback when SignalR is unavailable.
- Constrained the chat workspace to the viewport and made the message stream independently scrollable, so long answers no longer push the composer below the conversation.
- Preserved Enter to send and Shift+Enter for a new line. Separate tabs and sessions use independent SignalR stream invocations.

**Verification**

- `dotnet build EduPlatform.sln -c Release --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test tests/EduPlatform.Tests/EduPlatform.Tests.csproj -c Release --no-build --filter "TestCategory!=LiveGemini"` — 33 tests passed after adding streaming success/failure and anonymous hub authorization coverage.
- `dotnet test tests/EduPlatform.Tests/EduPlatform.Tests.csproj -c Release --filter "TestCategory=LiveGemini" --output Detailed` with local `GEMINI_API_KEY` — 1 live streaming test passed; Gemini returned streamed deltas and the service persisted one citation.
- Live browser verification still requires Bảo's authenticated local session, a Ready document, Neon configuration, and a valid Gemini API key.

### 2026-07-06 — Task 20: Chat MVC controller and workspace

**Owner**

- Bảo (implemented by Codex).

**Completed**

- Added authorized `ChatController` MVC actions for loading course sessions/messages, creating sessions, and sending questions with PRG redirects and user-facing validation errors.
- Added Chat Web view models and a responsive three-region Razor workspace: session history, message stream/composer, and citation source panel.
- Added native JavaScript for textarea resizing, prompt suggestions, loading/disabled state, keyboard submit, responsive drawers, citation focus, and latest-message scrolling.
- Changed the composer interaction so Enter submits and Shift+Enter inserts a new line.
- Added owner-only conversation deletion with confirmation; database cascade rules remove its messages and retrieval logs.
- Scoped the source panel to each assistant response, preserved query-specific similarity scores, applied a configurable minimum similarity threshold, and persisted only citation ranks actually referenced by Gemini (with top-result fallback when the model omits citation syntax).
- Added a course-details entry point for the learning assistant and an anonymous-access integration test.
- Deferred Cloudflare R2 configuration validation until an upload/download/delete operation actually uses storage, so opening controllers and testing Chat no longer requires R2 credentials.
- Applied the user-requested `gpt-taste` direction contextually within the fixed MVC, Bootstrap 5, CSP, and native JavaScript stack.

**UI/UX**

- Design Read: focused AI study room with strong reading hierarchy and source-first answers.
- Dials: `DESIGN_VARIANCE 7`, `MOTION_INTENSITY 4`, `VISUAL_DENSITY 7`.
- Deterministic direction: editorial split adapted to a product workspace, session rail, inline citations, source drawer, restrained text reveal and card-entry motion.
- Covered loading, empty, validation/error, disabled, hover, focus, active-session, mobile drawer, keyboard, and reduced-motion states.
- Desktop grid uses `280px / flexible conversation / 320px`; source drawer collapses below 1200px and session rail collapses below 992px; composer and suggestions become single-column on small mobile screens.
- `gpt-taste` AIDA, Tailwind, React, external font, and GSAP requirements were not applied because project rules require a product chat workflow, Bootstrap 5, MVC Razor, native JavaScript, and the existing CSP/font system.

**Verification**

- `dotnet build EduPlatform.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- Release test suite: 30 succeeded, 0 failed, and 1 opt-in live Gemini test skipped without its environment key.
- Added two R2 lifecycle tests proving missing credentials do not break service construction while storage operations still fail with a clear configuration error.
- Added service tests for cited-chunk selection and owner-authorized conversation deletion.
- `node --check EduPlatform.Web/wwwroot/js/chat.js`: passed.
- Razor compilation, anti-forgery coverage, anonymous login redirect, no user-visible en/em dash, focus styling, mobile breakpoints, and reduced-motion fallback verified.

**Remaining**

- Task 21: SignalR streaming ChatHub.
- Task 34: daily chat quota enforcement.
- Automated browser screenshot verification could not run because the installed in-app Browser runtime failed to initialize its local kernel assets; perform one manual desktop/mobile visual pass when opening the authenticated Chat page.

**Blocked**

- None for Task 20 implementation.

### 2026-07-05 — Task 19: ChatService RAG core

**Owner**

- Bảo (implemented by Codex).

**Completed**

- Added Chat DTOs and `IChatService` operations for sessions, messages, and RAG responses with citations.
- Added `IChatRepository` and `ChatRepository` for course access checks, session/message persistence, and course-scoped pgvector cosine search over ready document chunks.
- Added `ChatService` with ownership/authorization checks, question validation, query embedding, bounded context prompt construction, Gemini answer generation, atomic message/retrieval-log persistence, empty-context fallback, and automatic session titles.
- Added `IChatCompletionService` and `GeminiChatCompletionService` using the Gemini `generateContent` API.
- Extended Gemini embedding support with the correct `RETRIEVAL_QUERY` task type while preserving `RETRIEVAL_DOCUMENT` for ingestion.
- Added `GeminiOptions` and `ChatOptions`, updated dependency injection, and documented safe placeholder configuration in `appsettings.example.json`.
- Added six focused ChatService tests for RAG success/citations, empty context, foreign sessions, denied course access, Gemini failure, and session authorization.
- Added an opt-in live Gemini smoke test that exercises ChatService with real query embedding and `generateContent`, while using an in-memory repository boundary to avoid polluting Neon.

**Verification**

- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test --solution EduPlatform.sln --no-build --no-restore -- --output Normal`: 25 succeeded, 0 failed, and the opt-in live Gemini test skipped when its environment flag/key is absent.
- Live Gemini smoke test with the locally configured API key: 1/1 passed; returned a 3072-dimension query embedding, a non-empty answer, one citation, and persisted the expected in-memory message/retrieval records.
- Formatting verification passed for all Task 19 files.
- EF Core reports no pending model changes; Task 19 uses the existing Chat/Message/RetrievalLog schema.
- No direct Web-to-DAL reference or Razor Pages pattern was introduced.

**Remaining**

- Task 20: Chat MVC controller and views.
- Task 21: SignalR streaming ChatHub.
- Task 34: subscription-backed daily chat quota enforcement.
- Run a Neon-backed end-to-end RAG check after the Document UI has uploaded and processed a real course document.

**Blocked**

- None for Task 19 implementation.

### 2026-07-05 — Added and applied Package seed migration

**Owner**

- Local database setup completed by Codex.

**Completed**

- Added migration `20260705154759_SeedPackages` for the four existing Package seeds.
- Applied `InitialCreate`, `AddDocumentEmbeddingHnswIndex`, and `SeedPackages` to the configured Neon database.

**Verification**

- EF Core reports all three migrations as applied.
- `dotnet build EduPlatform.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test --solution EduPlatform.sln --no-build --no-restore`: passed.

**Remaining**

- None for initial table creation and Package seed data.

**Blocked**

- None.

### 2026-07-05 — Phase 3: Payment Integration (Tasks 28, 29, 30, 35)

**Owner**

- Nhân (implemented by Codex).

**Completed**

- Created `IPaymentRepository.cs` and `PaymentRepository.cs` in DAL.
- Created `PaymentDtos.cs` and `IPaymentService.cs` + `PaymentService.cs` in BLL.
- Implemented `IVNPayService` and `IMoMoService` with HMAC signature validation and URL generation.
- Created `PaymentController.cs` (MVC) with actions for CreatePayment, VNPayReturn, MoMoReturn, VNPayIpn, MoMoIpn, History, and Detail.
- Created Razor views for Payment History and Detail (`History.cshtml`, `Detail.cshtml`).
- Registered all options, services, and repositories in DI containers.
- Implemented idempotent callback handling and email confirmation in `PaymentService`.

**Verification**

- `dotnet build EduPlatform.sln`: passed with 0 warnings and 0 errors.

**Remaining**

- Keys for VNPay and MoMo are currently missing in `appsettings.json` (left null intentionally as per user request).
- Full end-to-end sandbox testing needs to be done once the API keys are provided.

**Blocked**

- VNPay sandbox credentials and callback URLs.
- MoMo sandbox credentials and callback URLs.

### 2026-07-04 — Phase 1: Package & Subscription (Tasks 23, 24, 25)

**Owner**

- Nguyên (implemented by Codex).

**Completed**

- Added `SeedPackages` to `AppDbContext.cs` for Free, Plus, Pro, and Max packages.
- Created `IPackageRepository.cs` and `PackageRepository.cs`.
- Created `ISubscriptionRepository.cs` and `SubscriptionRepository.cs`.
- Created `PackageDtos.cs` and `IPackageService.cs` + `PackageService.cs`.
- Created `SubscriptionDtos.cs` and `ISubscriptionService.cs` + `SubscriptionService.cs`.
- Registered new Repositories and Services in their respective `DependencyInjection.cs` files.

**Verification**

- `dotnet build EduPlatform.sln`: passed with 0 warnings and 0 errors.
- `dotnet test EduPlatform.sln`: 10/10 passed.

**Remaining**

- Implement `SubscriptionCourseQuotaService` (Nguyên Handoff) to replace `DeferredCourseQuotaService`.
- Task 26, 27, 31: Package and Subscription Web UI controllers and views.

**Blocked**

- None.

### 2026-07-02 — Task 11: AdminController (User CRUD & Excel Import)

**Owner**

- Nguyên (implemented by Codex).

**Completed**

- Added `EPPlus` to `Directory.Packages.props` and `EduPlatform.BLL.csproj` for Excel import.
- Created `AdminViewModels.cs` for Create, EditRole, Import and UserList.
- Created `AdminController.cs` for User CRUD and Excel Import with role `Admin`.
- Created Views: `Users.cshtml`, `CreateUser.cshtml`, `EditUser.cshtml`.
- Updated `_Layout.cshtml` to securely check roles before showing UI elements (e.g., hiding "Tạo khóa học" from Students).

**Verification**

- `dotnet build` passed.
- `dotnet test` passed (10/10).

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
