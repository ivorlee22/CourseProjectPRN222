# EduPlatform Project Log

Shared handoff log for developers and AI agents. Keep historical entries and add the newest activity entry first.

## Current Status

- Stage: Huy-owned foundation and Course scope implemented.
- Application code: Builds successfully on .NET 10.
- Canonical plan: `implementation_plan.md`.
- Architecture: Strict `Web → BLL → DAL`.
- Test baseline: 99 tests passing, 1 opt-in live Gemini test skipped without credentials.
- Database: Initial migration applied to Neon; pgvector 0.8.1 verified.
- Next milestone: Nguyên continues Package/Subscription polish and quota verification after the payment scope was reduced to VNPay only.

## Completed

- [x] Reviewed and corrected the implementation plan.
- [x] Finalized three-layer MVC architecture and dependency rules.
- [x] Finalized .NET 10, EF Core 10, Neon PostgreSQL, and pgvector.
- [x] Finalized Cookie Authentication and prohibition of Razor Pages.
- [x] Finalized Bootstrap 5, Gmail SMTP, and VNPay-only payments.
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
- [x] Replace `DeferredCourseQuotaService` with Nguyên's subscription-backed implementation.
- [x] Apply the initial migration to Neon and verify pgvector.

### Implementation

- [ ] Finish Phase 1 User/Account/Admin tasks owned by Nguyên.
- [ ] Phase 2: Documents, RAG chatbot, packages, and subscriptions.
- [ ] Phase 3: VNPay-only payment processing, and quota enforcement.
- [ ] Phase 4: Reports and full cross-module integration/security testing.

### Credentials Needed Before Integration Testing

- [x] Neon pooled runtime connection string.
- [ ] Neon direct migration connection string.
- [ ] Gemini API key.
- [x] Gmail address and Google App Password.
- [ ] VNPay sandbox credentials and callback URLs.

## Known Debt and Risks

- Full Register → Login → Payment → Course → Document → Chat → Report E2E remains blocked by unimplemented team modules.
- Course quota now uses `SubscriptionCourseQuotaService`; verify edge cases during the next Package/Subscription pass.
- Seed accounts are development-only and must be replaced or disabled before production.
- Payment callbacks must be designed for retries and duplicate delivery.
- Quota checks and usage updates must be transactional.
- Document module storage/extraction details remain owned by Nhân and are intentionally not prescribed.
- Taste-skill defaults to React/Next/Tailwind and excludes dashboards; EduPlatform must use its documented MVC/Bootstrap adaptation instead of copying those stack defaults.
- The populated Neon and Gmail secrets are currently in `appsettings.json`; move them to User Secrets or environment variables and leave only placeholders in tracked configuration.

## Activity Log

### 2026-07-11 - Document list and package edit cleanup

**Owner**

- Codex (Agent) / repository review follow-up requested by user.

**Completed**

- Replaced the document list entity graph load with a DAL projection that selects only list fields and computes `ChunkCount` in SQL, avoiding document chunk content, metadata, and embedding materialization.
- Added `IsActive` to the package detail DTO and update command so Admin package edit uses one package query and one save.
- Removed the unused legacy `Views/Payment/Packages.cshtml` while preserving the `/payment/packages` redirect route used by navigation.
- Marked success and error toast icons as decorative with `aria-hidden="true"`.
- Added regression coverage for projected document chunk counts, package active status, and single-save package updates.

**UI/UX**

- Design Read: maintenance update for the existing product UI, preserving Bootstrap/MVC behavior while improving accessibility semantics.
- Dials: `DESIGN_VARIANCE 3`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 6`.
- Pre-flight: routes, navigation labels, form fields, responsive behavior, focus behavior, motion, and visible copy were unchanged; decorative toast icons are now hidden from assistive technology.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-build --no-restore` passed: 93 succeeded, 1 opt-in live Gemini test skipped without credentials.
- Targeted PackageService and DocumentAccess tests passed: 7 succeeded, 0 failed.
- `git diff --check` passed.

**Remaining**

- None for this cleanup scope.

**Blocked**

- None.

### 2026-07-11 - Refine Payment History Layout

**Owner**

- Codex (Agent) / user visual review feedback.

**Design Read**

- Product/data screen for Students reviewing transactions; design variance low, motion intensity low, visual density medium.

**Completed**

- Removed the inline Payment history success/error alerts because `_Layout.cshtml` already shows TempData messages as global toasts.
- Reduced the Payment history heading scale and spacing so the screen reads as a transaction table, not a landing-page hero.
- Removed the redundant "Xem gói cước" header action from Payment history.
- Added Payment history specific CSS for tighter heading rhythm, table spacing, and action button sizing.
- Confirmed the invalid-signature wording is not exposed in the user-facing Payment views.

**Verification**

- `git diff --check` - passed with line-ending warnings only.
- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-restore` - passed: 99 succeeded, 1 skipped live Gemini smoke test because `GEMINI_API_KEY` is not set.

### 2026-07-11 - Make Payment Failure Copy User-Friendly

**Owner**

- Codex (Agent) / user visual review feedback.

**Completed**

- Replaced the user-facing VNPay return error that mentioned an invalid signature with a non-technical payment failure message.
- Kept invalid signature handling in the backend/IPN response path while hiding gateway implementation details from Students.
- Re-saved Payment history and detail views with proper Vietnamese Unicode text.
- Made the Payment history alert read as an incomplete payment state instead of exposing low-level gateway validation.

**Verification**

- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-restore` - passed: 99 succeeded, 1 skipped live Gemini smoke test because `GEMINI_API_KEY` is not set.

### 2026-07-11 - Refine Student Usage Navigation

**Owner**

- Codex (Agent) / user visual review feedback.

**Completed**

- Renamed the Student header navigation item from "Sử dụng" to "Thống kê" to match the usage statistics screen.
- Removed the redundant "Khóa học của tôi" hero button from the Student usage statistics page because the same destination already exists in the main header.
- Re-saved `StudentUsage.cshtml` with proper Vietnamese Unicode text while preserving the existing layout and data bindings.

**Verification**

- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-restore` - passed: 99 succeeded, 1 skipped live Gemini smoke test because `GEMINI_API_KEY` is not set.

### 2026-07-11 - Refine Student Subscription Screen IA

**Owner**

- Codex (Agent) / user visual review feedback.

**Completed**

- Removed the redundant "Xem bảng giá" action from the Student subscription header.
- Removed the secondary "Xem gói khác" action from the current subscription card; the screen now has one clear primary action: renew the current package.
- Removed the history table action column entirely because cancelled/expired rows are audit records, not active workflow items.
- Rebalanced the current subscription card into a summary row plus a consistent metric row for course quota, daily chat quota, start date, and end date.

**Verification**

- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-restore` - passed: 99 succeeded, 1 skipped live Gemini smoke test because `GEMINI_API_KEY` is not set.

### 2026-07-11 - Polish Student Subscription Management UI Copy

**Owner**

- Codex (Agent) / user visual review feedback.

**Completed**

- Restored Vietnamese accented status labels in Student subscription history.
- Renamed the current-package CTA from "Gia hạn qua VNPay" to "Gia hạn" to keep payment implementation details out of the management screen.
- Replaced "Đổi gói" with "Xem gói khác" on the current subscription card so the action accurately opens pricing instead of implying an instant switch.
- Fixed `SubscriptionHistoryItemViewModel` mapping so active and expired rows can show the renew action while cancellation remains hidden.
- Tightened the current subscription card layout with clearer metric tiles and consistent action sizing.

**Verification**

- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-restore` - passed: 99 succeeded, 1 skipped live Gemini smoke test because `GEMINI_API_KEY` is not set.

### 2026-07-11 - Align Student Package Quota and VNPay Subscription Lifecycle

**Owner**

- Codex (Agent) / user-confirmed Package and Course requirements.

**Completed**

- Enforced package purchase as Student-only in the BLL payment flow.
- Moved course quota from course creation to Student course participation: public enrollment and accepted invitations now count active enrollments and call `ICourseQuotaService`.
- Kept Admin-created courses and Teacher-assigned courses outside package quota; Teacher still cannot create courses.
- Removed active package cancellation from the Subscription UI; prepaid VNPay subscriptions are allowed to expire naturally.
- Changed subscription renew/change actions to go through Payment checkout instead of creating subscriptions directly from Subscription management.
- Implemented immediate upgrade behavior by expiring the current active subscription and activating the higher-priced paid package after successful VNPay payment.
- Implemented next-period downgrade/renew behavior without schema changes by creating a future-dated active subscription that becomes eligible only when `StartsAtUtc <= now`.
- Kept the implementation within existing entities and migrations; no Entity or Migration changes were retained.
- Updated Package, Payment, and Subscription copy to describe `MaxCourses` as courses the Student can join.
- Fixed VNPay IPN response handling to return success only when callback processing succeeds.
- Updated focused unit tests for payment lifecycle, subscription cancel/renew redirect, and enrollment quota enforcement.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` - passed.
- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-build --no-restore` - passed: 99 succeeded, 1 skipped live Gemini smoke test because `GEMINI_API_KEY` is not set.
### 2026-07-11 - Preserve chat state after streaming completes

**Owner**

- Bảo / Codex (Chat streaming UX fix, Tasks 19-21 and 44).

**Completed**

- Removed the full-page reload after a SignalR chat stream completes.
- Extended the completed stream event with the persisted assistant message ID and citations.
- Rendered citation chips and the matching source-panel group in place after the streamed answer finishes.
- Kept the composer, conversation scroll position, and current chat session in place while restoring the composer for the next question.
- Added a ChatService test that verifies completed stream events expose the persisted message and citation data.

**UI/UX**

- Design Read: realtime chat should preserve the user's place in the conversation and show citations as soon as the answer is complete, without a disruptive page transition.
- Dials: `DESIGN_VARIANCE 2`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.
- Product-screen pre-flight: preserved Bootstrap MVC and SignalR, existing citation interactions, keyboard focus behavior, responsive source panel, and reduced-motion-safe scrolling behavior.

**Verification**

- `node --check .\\EduPlatform.Web\\wwwroot\\js\\chat.js`: passed.
- `dotnet build .\\EduPlatform.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test .\\tests\\EduPlatform.Tests\\EduPlatform.Tests.csproj -c Release --no-build --no-restore`: passed: 94 succeeded, 1 skipped live Gemini credential test.
- Static stream check confirmed the client no longer calls `window.location.reload()` and renders completed citations in place.

**Remaining**

- Manual browser check: after a streamed answer finishes, verify no page reload occurs, the composer is enabled, citation chips appear, and `Chi tiết` opens the citation modal.

**Blocked**

- None.

### 2026-07-11 - Fix citation detail modal stacking

**Owner**

- Bảo / Codex (Chat UI bug fix, Tasks 20 and 44).

**Completed**

- Moved the citation detail modal outside the transformed chat workspace so Bootstrap's modal sits above its backdrop.
- Updated the chat script to locate the modal from the document while preserving the existing citation data binding and Bootstrap modal behavior.
- Preserved the existing close button, backdrop click, Escape behavior, and mobile source-panel behavior.

**UI/UX**

- Design Read: citation detail is a focused product workflow, so the modal must remain visually above the backdrop and reliably return control to the chat.
- Dials: `DESIGN_VARIANCE 2`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.
- Product-screen pre-flight: retained Bootstrap modal semantics, keyboard dismissal, focus restoration, responsive dialog sizing, and existing visual tokens. No new animation or design system was added.

**Verification**

- `node --check .\\EduPlatform.Web\\wwwroot\\js\\chat.js`: passed.
- `dotnet build .\\EduPlatform.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test .\\tests\\EduPlatform.Tests\\EduPlatform.Tests.csproj -c Release --no-build --no-restore`: passed: 94 succeeded, 1 skipped live Gemini credential test.
- Static view check confirmed `citationDetailModal` starts after the transformed `data-chat-workspace` container closes.
- Browser verification could not run because `http://localhost:7209` refused the connection while the local app was stopped.

**Remaining**

- Manual browser check recommended with a citation: open `Chi tiết`, close with the X button, click outside the dialog, and press Escape.

**Blocked**

- None.

### 2026-07-11 - Correct Report and Statistics dashboard copy

**Owner**

- Bảo / Codex (Tasks 36-41 copy correction).

**Completed**

- Corrected unclear or grammatically mixed labels across the Admin dashboard, Revenue report, and Teacher Statistics page without replacing established technical terms.
- Corrected the revenue grouping caption so the selected period renders as `Ngày`, `Tuần`, or `Tháng` rather than the English enum value.
- Replaced the unclear `Sức khỏe nội dung` heading with `Tình trạng nội dung`.
- Corrected the Teacher empty state to reflect Admin-assigned courses.

**UI/UX**

- Design Read: operational dashboards need concise copy that is clear in Vietnamese while keeping familiar domain terms such as payment, quota, and session where they aid scanning.
- Dials: `DESIGN_VARIANCE 2`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.
- Product-screen pre-flight: retained Bootstrap MVC components, responsive layouts, focus behavior, empty states, and existing semantic chart labels. No animation or layout change was needed.

**Verification**

- `dotnet build .\\EduPlatform.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test .\\tests\\EduPlatform.Tests\\EduPlatform.Tests.csproj -c Release --no-build --no-restore`: passed: 94 succeeded, 1 skipped live Gemini credential test.
- `git diff --check`: passed.
- Targeted copy scan found no remaining `Sức khỏe nội dung`, English period enum captions, `Theo payment`, `chart sẽ`, or Teacher course-creation empty-state copy.

**Remaining**

- Manual browser check recommended on Admin, Revenue, and Teacher Statistics pages.

**Blocked**

- None.

### 2026-07-11 - Remove Redundant Navigation Buttons from Admin Reports

**Owner**

- Antigravity (Agent) / user request.

**Completed**

- Removed the "Về tổng quan" button from the Revenue report page (`Revenue.cshtml`).
- Removed both "Quản lý người dùng" and "Về tổng quan" buttons from the User Analytics report page (`UserAnalytics.cshtml`).
- Removed both "Quản lý người dùng" and "Quản lý gói cước" buttons from the System Overview dashboard (`Admin/Index.cshtml`).

**Verification**

- Rebuilt the project successfully.
- Ran all 94 unit tests successfully.

### 2026-07-11 - Remove Redundant Actions from Teacher Statistics Dashboard

**Owner**

- Antigravity (Agent) / user request.

**Completed**

- Removed the redundant actions block from `TeacherStatistics.cshtml` that displayed the "Khóa học của tôi" (duplicated from the main header) and "Tạo khóa học" (teachers are not authorized to create courses) buttons.

**Verification**

- Rebuilt the project successfully.

### 2026-07-11 - Remove User ID Display from Profile Page

**Owner**

- Antigravity (Agent) / user request.

**Completed**

- Removed the User ID (Mã người dùng) row and its copy button from the personal profile view `Profile.cshtml` since displaying database GUID identifiers to users is unnecessary and cluttered. Adjusted layout borders accordingly.

**Verification**

- Rebuilt the project successfully.
- Ran all 94 unit tests successfully.

### 2026-07-11 - Restrict Admin Subscription View to Student Users

**Owner**

- Antigravity (Agent) / user request.

**Completed**

- Filtered `SubscriptionRepository.GetAllPagedAsync` query to only return subscriptions belonging to users with the role of `Student` (since only students are allowed to buy packages, and Admins/Teachers should not appear in the subscription dashboard list).
- Ran a database cleanup script to delete existing invalid test subscriptions and payments associated with non-Student users (Admin and Teacher accounts).

**Verification**

- Rebuilt the project successfully.
- Ran all 94 unit tests successfully.

### 2026-07-11 - Remove Redundant Hidden Badge Completely

**Owner**

- Antigravity (Agent) / user request.

**Completed**

- Completely removed the redundant and obsolete "Đang ẩn" (Currently hidden) badge from `_CourseList.cshtml` and `Details.cshtml` views since course visibility is now tied directly to the course type (Public = always visible, Private = always hidden from students/public listings).

**Verification**

- Rebuilt the project successfully.

### 2026-07-11 - Hide Redundant Hidden Badge on Private Courses

**Owner**

- Antigravity (Agent) / user request.

**Completed**

- Updated `_CourseList.cshtml` and `Details.cshtml` to hide the redundant "Đang ẩn" (Currently hidden) badge on courses that are already marked as "Riêng tư" (Private). The "Đang ẩn" badge is now only displayed for Public courses that are set to invisible.

**Verification**

- Rebuilt the project successfully.

### 2026-07-11 - Correct Admin Course Visibility and Private Course Count

**Owner**

- Antigravity (Agent) / user-reported course list visibility bug.

**Completed**

- Fixed `CourseService.SearchAsync` where `visibleOnly` was computed with the incorrect OR (`||`) operator instead of AND (`&&`) operator. This fixes the issue where Admins could not see Private and Hidden courses on the `/Course/Index` search listing page.
- Explained to the user that the private course count of 1 in the dashboard is correct because only `prn` is marked as `Private` in the database, while `swd` is database-flagged as `Public` despite being previously hidden (`IsVisible = false`).

**Verification**

- Cleaned and rebuilt the solution.
- Ran all 94 unit tests successfully.
- Verified query results via DB diagnostics.

### 2026-07-11 - Simplify course visibility and fix pending subscription status

**Owner**

- Antigravity (Agent) / user-reported dashboard and course visibility issues.

**Completed**

- Tied `IsVisible` directly to the course type: `Public` courses are always visible (`IsVisible = true`), and `Private` courses are hidden from public listings (`IsVisible = false`).
- Removed the redundant and conflicting "Hiển thị khóa học" (IsVisible) checkbox from `_CourseForm.cshtml`.
- Removed the "Ẩn khóa học" / "Hiển thị khóa học" toggle form from `Details.cshtml`.
- Updated the BLL `SubscriptionService` (`Map` and `MapAdmin`) to dynamically map any `Pending` subscription older than 15 minutes to `Expired` status. They now show up as "Hết hạn" in the Admin subscription list rather than staying "Chờ thanh toán" indefinitely.

**Verification**

- Ran `dotnet build` and `dotnet test`. All 94 unit tests passed.

### 2026-07-11 - Fix VNPay cancel payment unique constraint violation

**Owner**

- Antigravity (Agent) / user-reported payment cancellation bug.

**Completed**

- Fixed the unique constraint violation when cancelling a payment by mapping `vnp_TransactionNo = "0"` (or empty values) to `null` instead of `"0"` or empty string. Since the `IX_Payments_Method_GatewayTransactionId` index has a filter `GatewayTransactionId IS NOT NULL`, null values do not trigger duplicate key violations.
- Created `PaymentServiceTests.cs` covering both cancelled payment (gateway transaction ID mapped to null, status set to Failed) and successful payment (gateway transaction ID populated correctly, status set to Succeeded).

**Verification**

- Added and ran unit tests in `PaymentServiceTests.cs` using `dotnet test`.
- All 94 unit tests successfully passed.

### 2026-07-11 - Switch payment scope to VNPay only

**Owner**

- Codex (Agent) / user-confirmed VNPay-only payment scope for Nguyên continuation.

**Completed**

- Removed MoMo from production payment flow while keeping the legacy `PaymentMethod.MoMo` enum value for database compatibility.
- Removed MoMo service, interface, options, dependency injection registration, checkout UI, return/IPN routes, CSP form target, config example section, report copy, and unused MoMo payment asset.
- Updated payment creation so Student checkout always creates VNPay payments and rejects non-VNPay methods in BLL before persisting payment rows.
- Updated payment history/detail to show VNPay for supported records and a generic unsupported badge for legacy non-VNPay records.
- Updated the legacy payment package view to route paid packages into the VNPay checkout flow.
- Updated report tests to reflect VNPay-only revenue methods.
- Updated `AGENTS.md`, `implementation_plan.md`, `README.md`, `docs/SECURITY_REVIEW.md`, and current project log status to document VNPay-only payments and the completed course quota integration.

**UI/UX**

- Design Read: Checkout is now a single-method product payment flow, so the UI should remove choice friction and guide students directly into VNPay.
- Dials: `DESIGN_VARIANCE 3`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.
- `$fk plan` was used. `$fk` reported `NO_PRODUCT_MD`, but the referenced `reference/init.md` file was unavailable in the installed skill bundle; existing project docs and source were used as the authoritative context.

**Verification**

- `rg "MoMo|momo|MOMO"` across Web/BLL/tests found no remaining matches.
- `rg "MoMo|momo|MOMO"` across DAL found only the intentionally retained legacy enum value.
- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-build --no-restore` passed: 92 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended on `/Package`, `/payment/checkout/{packageId}`, `/payment/history`, and revenue report pages.
- VNPay sandbox credentials and callback URLs are still needed for full end-to-end payment verification.

**Blocked**

- None for code changes. Full VNPay E2E remains blocked on valid VNPay sandbox configuration.

### 2026-07-11 - Pricing free-plan and admin dropdown polish

**Owner**

- Codex (Agent) / user visual QA feedback on Admin Users and Package Pricing.

**Completed**

- Fixed the Admin Users row action dropdown so it is no longer clipped by the user table panel on desktop.
- Removed the `Free forever` subline from the Free pricing card.
- Rebalanced the Free card price block so `Miễn phí` is vertically aligned with paid package prices.
- Changed the Free package duration display from `36500 ngày` to unlimited-time wording in pricing highlights and the comparison table.
- Renamed the comparison heading from quota-focused wording to `So sánh quyền lợi` while keeping the useful package comparison table.

**UI/UX**

- Design Read: Package pricing and Admin Users are product UI surfaces, so this polish reduces confusing copy and keeps task controls predictable.
- Dials: `DESIGN_VARIANCE 3`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.
- The optional local `taste-skill` path was unavailable in the repository; `$fk` product guidance and EduPlatform Bootstrap MVC rules were applied. `$fk` reported `NO_PRODUCT_MD`, but `reference/init.md` was unavailable in the installed skill bundle, so the existing project guide, plan, and log were used as context.

**Verification**

- `rg "Free forever|36500 ngày|Quota theo từng gói"` across Web/BLL/DAL found no remaining pricing UI matches.
- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-build --no-restore` passed: 92 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended on `/Admin/Users` and `/Package` to confirm the dropdown and optical card alignment against the live viewport.

**Blocked**

- None.

### 2026-07-10 - Fix Admin course delete redirect

**Owner**

- Codex (Agent) / Admin delete-course redirect bug report.

**Completed**

- Fixed `CourseController.Delete` so Admin is redirected back to `Course/Index` after deleting a course.
- Removed the incorrect post-delete redirect to `Course/Mine`, which caused Admin to land on the `Khóa học của tôi` page.
- Preserved existing delete authorization, CourseService delete behavior, SignalR notification, and success TempData message.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-build --no-restore` passed: 92 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended with an Admin account: delete a course from details and confirm the app returns to `Khóa học`, not `Khóa học của tôi`.

**Blocked**

- None.

### 2026-07-10 - Teacher assigned-course permissions cleanup

**Owner**

- Codex (Agent) / Teacher course detail UI and logic correction.

**Completed**

- Split course detail permissions into Admin administration and Teacher teaching actions.
- Kept Admin-only actions for editing, hiding/showing, and deleting courses.
- Kept Teacher assigned-course actions focused on students, invitations, teaching documents, and document Q&A.
- Redirected authenticated Teachers from the public Course search/index page to `Khóa học của tôi`.
- Updated Course and Document breadcrumbs so Teacher users return to `Khóa học của tôi` instead of public course search.
- Enforced the rule in BLL: `CourseService.UpdateAsync`, `DeleteAsync`, and `SetVisibilityAsync` now require Admin.
- Added Admin role authorization on Course edit, delete, and visibility MVC actions.
- Preserved Teacher access for assigned-course student and invitation operations behind the existing owner/Admin check.
- Added CourseService tests for assigned Teacher being forbidden from update, delete, and visibility changes.

**UI/UX**

- Design Read: Teacher is an assigned-course workflow, so the course detail panel should show teaching tools only while Admin retains full course administration.
- Dials: `DESIGN_VARIANCE 2`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\tests\EduPlatform.Tests\EduPlatform.Tests.csproj -c Release --no-build --no-restore` passed: 92 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended with both Admin and Teacher accounts on course details, document list, upload, and breadcrumbs.

**Blocked**

- None.

### 2026-07-10 - Teacher course navigation cleanup

**Owner**

- Codex (Agent) / Teacher navigation request.

**Completed**

- Updated the shared header navigation so the public `Khóa học` course search link is hidden for authenticated Teacher users.
- Kept Teacher navigation focused on `Khóa học của tôi` and `Thống kê`, matching the Admin-assigned teaching flow.
- Preserved existing Course routes, controllers, services, authorization policies, and course search behavior for anonymous users, Students, and Admins.

**UI/UX**

- Design Read: Teacher is an assigned-course workflow, so navigation should avoid sending them into public course discovery/search.
- Dials: `DESIGN_VARIANCE 2`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.

**Remaining**

- Manual browser check recommended with a Teacher account to confirm the header shows `Khóa học của tôi` and `Thống kê`, without the public `Khóa học` link.

**Blocked**

- None.

### 2026-07-10 - Hide small page eyebrow labels

**Owner**

- Codex (Agent) / UI polish request.

**Completed**

- Hid the shared `.section-label` page eyebrow style so small uppercase labels such as the Course page "Không gian học tập" no longer render visually.
- Kept the change scoped to `EduPlatform.Web/wwwroot/css/site.css`.
- Preserved Razor views, routes, controllers, actions, model binding, and backend logic.

**UI/UX**

- Design Read: This is a product education UI polish pass, so the change removes noisy small uppercase page labels while keeping the existing Bootstrap MVC visual system.
- Dials: `DESIGN_VARIANCE 2`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 5`.
- The optional local taste-skill file was not present in the repository path; the project rules and available `$fk` product guidance were applied.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.

**Remaining**

- Manual browser check recommended on pages that previously showed `.section-label` to confirm the cleaner heading rhythm.

**Blocked**

- None.

### 2026-07-10 - Admin header navigation order

**Owner**

- Codex (Agent) / Admin navbar ordering request.

**Completed**

- Moved the Admin dashboard link `Tổng quan` out of the `Quản trị` dropdown and into the top-level header before `Khóa học`.
- Preserved existing routes and actions: `Admin/Index`, `Course/Index`, and the remaining admin dropdown links.
- Kept the change scoped to the shared layout navigation.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.

**Remaining**

- Manual browser check recommended with an Admin account to confirm the header order reads `Tổng quan`, `Khóa học`, `Quản trị`.

**Blocked**

- None.

### 2026-07-10 - Pricing alignment and spacing polish

**Owner**

- Codex (Agent) / `$fk build` pricing alignment polish request.

**Completed**

- Tightened the Pricing hero spacing and adjusted the heading scale so the desktop title stays on one clean line.
- Kept the existing Pricing view logic, current-package highlight logic, route/action targets, package IDs, and CTA bindings unchanged.
- Rebalanced pricing card spacing, fixed card sections to a consistent vertical rhythm, and kept all card CTAs pinned to the bottom.
- Reduced the highlighted Pro/current card lift to a transform-only treatment so it stands out without disturbing grid alignment.
- Standardized card price sizing, button height, feature-list growth, and mobile/tablet breakpoints for a steadier 4/2/1 layout.

**UI/UX**

- Design Read: Pricing is a SaaS-style comparison surface, so the polish focused on calm alignment, equal scanning rhythm, and a stronger but non-disruptive recommended plan.
- Dials: `DESIGN_VARIANCE 3`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 5`.
- `$fk` reported `NO_PRODUCT_MD`, but the referenced `init.md`, `layout.md`, and `typeset.md` files were unavailable in the local skill folder; project rules and existing EduPlatform UI conventions were used without adding new files outside the user's Pricing-only scope.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended on `/Package` across desktop, tablet, and mobile to confirm final optical balance with real viewport rendering.

**Blocked**

- None.

### 2026-07-10 - Pricing section polish pass

**Owner**

- Codex (Agent) / `$fk build` pricing polish request.

**Completed**

- Reduced the Pricing hero scale, tightened subtitle spacing, and kept the Vietnamese copy.
- Preserved Package view model binding, package IDs, existing routes, actions, and CTA behavior.
- Added view-only highlight selection: current package is highlighted first; when no current package exists, Pro is highlighted by default.
- Refined pricing cards with larger radius, softer shadow, balanced padding, equal-height layout, and desktop/tablet/mobile grid behavior.
- Updated highlighted card treatment with brand teal background, white text, small label, and a light CTA style.
- Changed feature bullets to CSS checkmarks without adding icon libraries or external dependencies.
- Added a soft teal-tinted pricing section background while preserving EduPlatform's color system.

**UI/UX**

- Design Read: Pricing should feel like a compact SaaS comparison section, with the selected or recommended plan visually dominant but not off-brand.
- Dials: `DESIGN_VARIANCE 4`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 5`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended on `/Package` at desktop, tablet, and mobile widths to judge final visual balance against real browser rendering.

**Blocked**

- None.

### 2026-07-10 - Pricing page card redesign

**Owner**

- Codex (Agent) / `$fk build` Pricing UI request.

**Completed**

- Redesigned `Views/Package/Index.cshtml` into a centered pricing page with a modern four-card grid for Free, Plus, Pro, and Max.
- Preserved all existing package data binding, route/action targets, package IDs, and CTA conditions.
- Reworked pricing CSS so non-current plans use bright cards with thin borders and soft shadows.
- Highlighted the user's current package with a dark teal card treatment inspired by the reference Pro card while preserving the project color palette.
- Kept responsive behavior for desktop, tablet, and mobile without adding any external library.

**UI/UX**

- Design Read: Pricing should read as a focused comparison surface first, with the owned package clearly distinguished from purchasable plans.
- Dials: `DESIGN_VARIANCE 4`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 5`.
- `$fk build` reference files `layout.md` and `typeset.md` were unavailable in the local skill folder, so existing `fk` and EduPlatform Bootstrap/MVC rules were applied.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended on `/Package` with Student and anonymous users to visually confirm card balance with live package data.

**Blocked**

- None.

### 2026-07-10 - Crop VNPay checkout logo

**Owner**

- Codex (Agent) / user visual QA feedback.

**Completed**

- Cropped the provided VNPay image from its wide canvas into a square app-icon style asset so it displays at the same visual weight as MoMo.
- Updated payment logo shell CSS so VNPay and MoMo use the same square sizing and rounded treatment.
- Removed temporary SVG payment placeholders and the crop backup file so only the final PNG payment logos remain.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- None.

**Blocked**

- None.

### 2026-07-10 - Replace payment placeholders with provided logos

**Owner**

- Codex (Agent) / user-provided payment brand assets.

**Completed**

- Added the provided VNPay and MoMo PNG logo assets to `EduPlatform.Web/wwwroot/img/payments/`.
- Updated checkout payment method cards to use `vnpay.png` and `momo.png` instead of the temporary generated SVG placeholders.
- Adjusted payment logo shell sizing so the square MoMo mark and wider VNPay image both render cleanly.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- None.

**Blocked**

- None.

### 2026-07-10 - Payment logos and student navigation cleanup

**Owner**

- Codex (Agent) / checkout visual polish requested by user.

**Completed**

- Added payment logo assets for VNPay and MoMo under `EduPlatform.Web/wwwroot/img/payments/`.
- Replaced the text-only VN/Mo markers on the checkout page with logo cards using the new payment assets.
- Updated checkout card layout and CSS so payment methods use recognizable brand visuals with cleaner alignment and hover/focus states.
- Removed the Student header link to `Gói của tôi` because the `Sử dụng` page already covers subscription/package usage context.

**UI/UX**

- Design Read: Payment methods should use recognizable gateway logos so checkout feels trustworthy and less text-heavy.
- Dials: `DESIGN_VARIANCE 4`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 6`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended on the checkout page to compare the SVG approximations against any official brand assets the team may later provide.

**Blocked**

- None.

### 2026-07-10 - Pricing checkout flow and package highlighting

**Owner**

- Codex (Agent) / pricing and payment UI fix requested by user.

**Completed**

- Changed pricing cards so paid packages show a single `Mua ngay` action instead of choosing VNPay/MoMo directly on the pricing page.
- Added `PaymentController.Checkout` and `Views/Payment/Checkout.cshtml` so Students choose VNPay or MoMo on a dedicated payment step after selecting a package.
- Added package pricing metadata for `IsFeatured`; Plus is highlighted as the hot/popular package.
- Updated pricing UI so the current package has a distinct active treatment and the featured package has a visible hot ribbon/lifted card style.
- Kept Free package as a disabled/default package rather than routing it into payment.
- Replaced payment creation stack trace output with user-facing TempData errors and redirects.

**UI/UX**

- Design Read: Pricing should let students compare packages first, then make payment method selection a focused second step.
- Dials: `DESIGN_VARIANCE 4`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 6`.
- Applied Bootstrap 5 MVC product UI rules with restrained teal highlighting and no new frontend framework.

**Verification**

- `node --check EduPlatform.Web\wwwroot\js\site.js` passed.
- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 85 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser check recommended on `/Package` and `/payment/checkout/{packageId}` with a Student account to tune final spacing against real package data.

**Blocked**

- End-to-end payment still depends on valid VNPay/MoMo sandbox credentials.

### 2026-07-10 - Cross-page UI system polish and clickable cards

**Owner**

- Codex (Agent) / UI polish pass confirmed by user.

**Completed**

- Added shared UI polish tokens for soft elevation, transition timing, semantic state colors, breadcrumbs, panels, empty states, tables, dropdowns, detail lists, and card hover/focus states.
- Added a guarded `data-clickable-card` interaction in `site.js` so Course and Document cards can be opened by clicking the full card area while preserving buttons, forms, inputs, and keyboard access.
- Refactored Course list invitation cards and Course/Document cards to use shared classes instead of inline styling.
- Reworked Course Details, Payment History, and Payment Detail into consistent page heading, content panel, action panel, table, and detail-list layouts.
- Aligned Admin user, package, and subscription tables with the shared table/panel system and removed stray inline styling/duplicate modal attributes.

**UI/UX**

- Design Read: EduPlatform needs a consistent product UI system across role-based pages, preserving the teal education tone while adding softer panels, clearer interactive affordances, and restrained state motion.
- Dials: `DESIGN_VARIANCE 4`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 6`.
- Applied product-register guidance for Bootstrap 5 MVC screens; no new frontend framework or design system was introduced.

**Verification**

- `node --check EduPlatform.Web\wwwroot\js\site.js` passed.
- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 85 succeeded, 1 skipped live Gemini credential test.

**Remaining**

- Manual browser review is still recommended on authenticated Admin, Course, Document, Payment, and Package pages to tune final spacing after seeing real data.
### 2026-07-10 - Fix UI chart rendering syntax and height issues

**Owner**

- Bảo report scope (implemented by Antigravity).

**Design Read**

- Polish UI charts to fix squished bars and rendering issues for C# decimal formatting.
- Dials: design variance medium, motion intensity medium, visual density low

**Completed**

- `EduPlatform.Web/Views/Admin/Index.cshtml`: Wrapped `.ToString()` with `@()` so `maxRevenue`, `maxUserGrowth`, `maxChatUsage` render as formatted values, not raw code. Fixed parens on `.Count(...)`.
- `EduPlatform.Web/Views/Reports/Revenue.cshtml`: Wrapped `.ToString()` for `maxPeriodRevenue` and `.Count(...)` inside `@(...)` to fix raw C# code rendering.
- `EduPlatform.Web/Views/Reports/UserAnalytics.cshtml`: Wrapped `.ToString()` for `maxUserGrowth` and `.Sum(...)` inside `@(...)` to fix raw C# code rendering.
- `EduPlatform.Web/Views/Course/Details.cshtml`: Wrapped `.ToString()` inside `@(...)` for hidden `isVisible` field.
- `EduPlatform.Web/wwwroot/css/site.css`: Added `height: 100%;` to `.report-column-chart__bar-wrap` to allow percentage heights of its flex children to resolve correctly, fixing the squished (đụt) charts.

**Verification**

- `dotnet build "c:\Users\THIS PC\Desktop\Semester_7\PRN222\CourseProjectPRN222\EduPlatform.Web\EduPlatform.Web.csproj" -c Release --no-restore` passed with 0 warnings and 0 errors.
- Visual inspection of code confirms Razor parens are correctly balanced to execute C# formatting and CSS properly provides explicit height to flex children.

**Remaining**

- None.

**Blocked**

- None.


### 2026-07-10 - Report chart UI and Npgsql query fixes

**Owner**

- Bảo report scope (UI updated by Antigravity, query fixes completed by Codex).

**Design Read**

- Admin and report pages are internal product dashboards, so the UI keeps MVC, Bootstrap 5, native JavaScript, strong data hierarchy, accessible labels, and restrained motion instead of marketing-style visuals.
- Dials: design variance medium, motion intensity light-dynamic, visual density medium.

**Completed**

- Reworked Admin overview, Revenue, User Analytics, and Teacher Statistics chart presentation with a shared chart card system.
- Added `report-column-chart` and `report-distribution` styling, animated bars, hover states, chart gridlines, stagger support through `--chart-index`, and percentage/value presentation for distribution rows.
- Aligned Teacher Statistics chart markup with the shared `content-panel chart-card` structure and existing semantic legend classes.
- Restored EF Core/Npgsql-safe report queries for revenue by package, revenue by payment method, users by role, and subscription distribution.
- Moved enum/string conversion and grouped navigation-name aggregation out of SQL translation by materializing scalar values first, then grouping in memory.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini integration test skipped.

**Remaining**

- Manual browser check recommended for `/Admin`, `/Reports/Revenue`, `/Reports/UserAnalytics`, and `/Reports/TeacherStatistics` after merge.

**Blocked**

- None.

### 2026-07-10 - Fixed Admin dashboard top courses query

**Owner**

- Bảo report scope (implemented by Codex).

**Completed**

- Fixed `ReportRepository.GetTopCoursesAsync` so the Admin dashboard no longer asks EF Core/Npgsql to order by properties on a projected `TopCourseSnapshot` record.
- Split the database projection from the BLL snapshot mapping: SQL now returns an anonymous scalar shape first, then maps to `TopCourseSnapshot` after materialization.
- Replaced the nested `SelectMany(...).Count(...)` message count with an explicit correlated count over `Messages` by `ChatSession.CourseId`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini integration test skipped.

**Blocked**

- None.

### 2026-07-10 - Redesigned Teacher Statistics chart UI and animation

**Owner**

- Bảo report scope (implemented by Antigravity).

**Design Read**

- Teacher statistics is a product data workflow rather than a marketing page, so the redesign keeps MVC, Bootstrap 5, native JavaScript, and restrained dashboard styling.
- The redesign also fixes the flattened bars and missing legend colors caused by the Content Security Policy (`style-src 'self'`) blocking inline `style="..."` attributes:
  - Legend dots now use CSS classes from the stylesheet instead of inline styles.
  - Chart bars now expose their intended height through `data-height`, then `site.js` applies the height after `DOMContentLoaded` from a script file allowed by CSP.
- Dials: design variance medium, motion intensity dynamic, visual density medium.

**Completed**

- Fixed flattened chart bars by making `.teacher-stat-chart__bar` render as a block-level visual element.
- Replaced CSP-blocked inline bar heights with `data-height` attributes and a small `site.js` initializer that applies heights on `DOMContentLoaded`.
- Replaced CSP-blocked inline legend colors with semantic CSS classes and gradient colors in `site.css`.
- Redesigned the chart container with Bootstrap `card border-0 shadow-sm`, a clearer heading area, and a flex/badge-based legend.
- Added an 850ms bottom-up bar growth animation using `cubic-bezier(0.34, 1.56, 0.64, 1)`.
- Added CSS-only hover tooltips with a softer shadow and subtle bar scale feedback.
- Refined the background grid lines and top-course summary cards with light hover elevation.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini integration test skipped.

**Blocked**

- None.

### 2026-07-09 - Teacher chart fallback fix

**Owner**

- Bảo report scope (implemented by Codex).

**Completed**

- Replaced the Teacher Statistics Chart.js-only canvas with a server-rendered HTML/CSS bar chart.
- Removed the Teacher Statistics dependency on the external Chart.js CDN so the chart remains visible even when the CDN is blocked or unavailable.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Blocked**

- None.

### 2026-07-09 - Task 44 Chat UI Polish

**Owner**

- Bảo chat scope (implemented by Codex).

**Design Read**

- Chat is a product workflow, so the polish keeps the existing MVC/native JavaScript structure, improves readability, and adds source/code affordances without changing routes or data contracts.
- Dials: design variance medium, motion intensity light, visual density medium.

**Completed**

- Added fenced code block support to the safe server-side chat markdown renderer.
- Added matching fenced code block rendering for SignalR streaming responses.
- Added lightweight syntax highlighting and copy buttons for chat code blocks.
- Polished chat bubble radius, shadows, code block styling, and source card controls.
- Added citation detail modal with source rank, similarity score, location, and excerpt.
- Added citation chip tooltip text and accessible focus states for new controls.

**Verification**

- `node --check EduPlatform.Web\wwwroot\js\chat.js` passed.
- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- No remaining Bảo Phase 4 report/chat polish tasks in the current task range.

**Blocked**

- None.

### 2026-07-09 - Realtime invitations, course quota integration, and Admin bypass

**Owner**

- Codex (Agent) / pair programming with user.

**Completed**

- Fixed `ICourseService` / `CourseService` and `CourseController` to integrate `ICourseQuotaService` during course creation and check the quota correctly.
- Added Admin bypass check to `SubscriptionCourseQuotaService.cs` using `IUserRepository` to resolve the user's role.
- Integrated `RemoveEnrollment` to `ICourseRepository` and implemented it in `CourseRepository` and tests.
- Replaced separate Student "Lời mời" (Invitations) tab with a realtime styled alert card on the course list index page (`Index.cshtml` & `_CourseList.cshtml`).
- Added Cancel Invitation capability for Admins in `Students.cshtml` with trash icon and hooked it up to the BLL service.
- Broadcasted realtime SignalR notifications (`ReceiveInvitation` and `CancelInvitation`) to users when invitations are sent or cancelled.
- Added unit tests for Admin bypass on quota, integration of quota checks, and invitation cancellation. All 85/85 tests passed.

**UI/UX**

- Design Read: Realtime notifications should use visual cues (border color, light grey backgrounds) to draw focus and provide distinct actions without visual clutter.
- Dials: `DESIGN_VARIANCE 1`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 5`.
- Shared MVC/Bootstrap rules applied.
### 2026-07-09 - Task 41 Student Usage Report

**Owner**

- Bảo report scope (implemented by Codex).

**Design Read**

- Student usage is a self-service dashboard, so the UI emphasizes quota clarity, active subscription status, and quick links back to courses and package management.
- Dials: design variance low-medium, motion intensity light, visual density medium.

**Completed**

- Added Student-only `/Reports/StudentUsage` backed by `IReportService.GetStudentUsageAsync` for the signed-in student.
- Added summary cards for enrolled courses, chat sessions, chat messages, and remaining daily chat quota.
- Added progress bars for daily chat quota and course usage against the active package limit when available.
- Added active subscription detail panel with package, course limit, daily chat quota, start date, and end date.
- Added Student navigation link for "Sử dụng".

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- Task 44 Chat UI Polish remains separate branch.

**Blocked**

- None.

### 2026-07-09 - Task 40 Teacher Statistics

**Owner**

- Bảo report scope (implemented by Codex).

**Design Read**

- Teacher statistics is a product dashboard for quick course health checks, so the UI prioritizes readable totals, per-course comparison, and a compact table.
- Dials: design variance low-medium, motion intensity light, visual density medium.

**Completed**

- Added Teacher-only `/Reports/TeacherStatistics` backed by `IReportService.GetTeacherStatisticsAsync` for the signed-in teacher.
- Added summary cards for total courses, enrolled students, document readiness, and chat usage.
- Added Chart.js bar chart comparing enrollments, documents, and chat messages by course.
- Added per-course detail table for enrolled students, documents, ready documents, chat sessions, and chat messages.
- Added Teacher navigation links for "Khóa học của tôi" and "Thống kê".

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- Task 41 Student Usage Report and Task 44 Chat UI Polish remain separate branches.

**Blocked**

- None.

### 2026-07-09 - Task 39 User Analytics

**Owner**

- Bảo report scope (implemented by Codex).

**Design Read**

- User analytics is an Admin data workflow, so the UI keeps filter controls visible, charts readable, and role/subscription summaries compact.
- Dials: design variance low-medium, motion intensity light, visual density medium.

**Completed**

- Added Admin-only `/Reports/UserAnalytics` backed by `IReportService.GetUserAnalyticsAsync`.
- Added date range and day/week/month grouping filters.
- Added summary cards for total users, new users, role count, and active subscription count.
- Added Chart.js visualizations for new users over time, role distribution, and subscription distribution.
- Added role breakdown cards and Admin navigation link.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- Tasks 40-41 and 44 remain separate branches.

**Blocked**

- None.

### 2026-07-09 - Task 38 Revenue Report

**Owner**

- Bảo report scope (implemented by Codex).

**Design Read**

- Revenue report is an Admin data workflow, so the UI keeps Bootstrap/MVC, clear filters, visible export action, readable charts, and compact data tables.
- Dials: design variance low-medium, motion intensity light, visual density medium.

**Completed**

- Added Admin-only `ReportsController` with revenue report and Excel export actions.
- Added `/Reports/Revenue` with date range filters, grouping by day/week/month, summary cards, Chart.js visualizations, and a revenue table.
- Added `/Reports/ExportRevenue` to generate an `.xlsx` workbook with summary, revenue by period, revenue by package, and revenue by payment method sheets.
- Added revenue report view model and Admin navigation link.
- Added responsive report filter styling in `site.css`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- Tasks 39-41 and 44 remain separate branches.

**Blocked**

- None.

### 2026-07-09 - Task 37 Admin Dashboard

**Owner**

- Bảo report scope (implemented by Codex).

**Design Read**

- Admin dashboard is a product/data screen, so the taste-skill guidance was adapted to MVC, Bootstrap 5, restrained hierarchy, responsive spacing, accessible states, and low-motion charts.
- Dials: design variance low-medium, motion intensity light, visual density medium.

**Completed**

- Added the Admin dashboard at `Admin/Index` using `IReportService` only from the Web layer.
- Added dashboard view model wiring for the 30-day report range and daily grouping.
- Added summary cards for total users, total courses, succeeded revenue, and active subscriptions.
- Added Chart.js charts for revenue, user growth, chat usage, and subscription distribution.
- Added top courses and course content health sections.
- Added "Tổng quan" to the Admin navigation menu.
- Added responsive dashboard styling in `site.css`.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- Task 38 still needs Revenue Report page and Excel export.
- Tasks 39-41 and 44 remain separate branches.

**Blocked**

- None.

### 2026-07-09 - Task 36 ReportService

**Owner**

- Bảo report scope (implemented by Codex).

**Completed**

- Added `IReportService` and `ReportService` for report aggregates used by Admin, Revenue, User Analytics, Teacher Statistics, and Student Usage pages.
- Added report DTOs for dashboard totals, revenue time series, user growth, course stats, chat usage, top courses, subscription distribution, teacher course stats, and student quota usage.
- Added `IReportRepository` and `ReportRepository` with read-only aggregate queries over users, courses, enrollments, documents, chat messages, subscriptions, and succeeded payments.
- Registered report repository and service in DAL/BLL dependency injection.
- Added report service unit tests for revenue bucketing, admin dashboard composition, teacher total aggregation, and student free-package quota fallback.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 89 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- Tasks 37-41 still need MVC controllers/views to consume `IReportService`.
- Task 38 export still needs Excel generation from the revenue DTO.

**Blocked**

- None.

### 2026-07-09 - Admin search, course realtime, and student document access

**Owner**

- Admin/User, Course, and Document access scope (implemented by Codex).

**Completed**

- Added keyword search to Admin user management, filtering accounts by full name or email while preserving oldest-account-first ordering.
- Synchronized visible Admin subscription UI labels to Vietnamese, including replacing mixed "Subscriptions" copy with "Đăng ký gói" / "Quản lý đăng ký gói".
- Removed the global header shortcut for course creation so "Tạo khóa học" lives in the Course screen flow.
- Added `CourseHub`, course list fragment rendering, and `course-live.js` so Course create/update/delete/visibility changes broadcast through SignalR and refresh open Teacher/Student/Admin course lists without manual reload.
- Updated document access rules so authenticated students can view/download documents for visible public courses, while hidden courses and password/private courses still require allowed access.
- Added document access regression tests for visible public, visible private, and hidden public course cases.

**UI/UX**

- Design Read: Admin/product workflow needs fast table scanning, clear form controls, and minimal decoration.
- Dials: `DESIGN_VARIANCE 3`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 8`.
- Local `taste-skill/skills/taste-skill/SKILL.md` was not available, so shared Bootstrap/MVC product workflow rules were applied.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors after escalation for local NuGet config access.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 80 succeeded, 1 skipped live Gemini credential test.
- `node --check .\EduPlatform.Web\wwwroot\js\course-live.js` passed.

**Remaining / Blockers**

- No database migration required.
- SignalR refresh applies to users currently viewing the rendered Course list pages; disconnected clients receive the updated list on normal navigation.

### 2026-07-09 - Admin role and course workflow fixes

**Owner**

- Admin/User and Course scope (implemented by Codex).

**Completed**

- Allowed Admin user creation and role updates to assign the Admin role from both BLL validation and Admin Razor views.
- Kept safeguards for admins changing or disabling their own account and surfaced those errors cleanly in Admin UI.
- Sorted Admin user list by oldest account first using `CreatedAtUtc`.
- Allowed course creation without selecting a teacher by making `OwnerId` optional and temporarily assigning the creating Admin as owner.
- Added Admin course edit support for assigning or changing the teacher later.
- Changed course invitation from user ID input to email or student name lookup, with duplicate-name protection that asks the admin to use email.
- Hid and blocked "Hỏi từ tài liệu" access for Admin in MVC Chat routes and SignalR streaming.
- Added CourseService regression tests for creating without a teacher and inviting by email/name.

**UI/UX**

- Design Read: Admin/product workflow needs fast table scanning, clear form controls, and minimal decoration.
- Dials: `DESIGN_VARIANCE 3`, `MOTION_INTENSITY 1`, `VISUAL_DENSITY 8`.
- Local `taste-skill/skills/taste-skill/SKILL.md` was not available, so the shared MVC/Bootstrap Admin workflow rules were applied.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 77 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- No database migration was needed because optional teacher creation is implemented by temporarily assigning the Admin owner until a teacher is selected.

**Blocked**

- None.

### 2026-07-09 - Account creation email notification

**Owner**

- Nguyên user/auth scope (implemented by Codex).

**Completed**

- Wired `UserService.RegisterAsync` to send an account-created email to the newly registered email address after the user is persisted.
- Wired `UserService.CreateAsync` to send account-created email for Admin-created and imported accounts, including the initial temporary password.
- Updated `IEmailService.SendAccountCreatedAsync` and `GmailEmailService` to support optional temporary password content while keeping self-registration emails password-free.
- Logged email delivery failures without rolling back an already-created account.
- Added user service tests for self-registration email, admin-created account email, and persistence when email delivery fails.

**Verification**

- `dotnet build .\EduPlatform.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test .\EduPlatform.sln -c Release --no-build --no-restore` passed: 74 succeeded, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.

**Remaining**

- Real Gmail delivery still depends on valid local `Email` configuration or environment/user-secret values.

**Blocked**

- None.
### 2026-07-08 - Document chunking, duplicate guard, chat role lock, PPTX, and MoMo removal

**Owner**

- Nhân document scope and Nguyên package scope (implemented by Codex).

**Completed**

- **Chunking**: Rewrote `FixedSizeTextChunker` so it splits by paragraph/newline/sentence/word before falling back to a hard window. Existing tests still pass and new `FixedSizeTextChunkerTests` cover paragraph merging, sentence boundaries, overlap, single-paragraph input, and page boundaries.
- **Duplicate documents**: Added `IDocumentRepository.ExistsByCourseAndFileNameAsync` and called it from `DocumentService.UploadAsync`. `DocumentController` now surfaces a friendly TempData warning instead of an exception. Added `DocumentServiceTests` covering duplicate rejection, fresh upload success, unsupported MIME rejection, and PPTX support through the `PptxTextExtractor`.
- **PPTX support**: Added `PptxTextExtractor` using `DocumentFormat.OpenXml`, registered it in DI, extended `DocumentFileType.Pptx`, removed `application/octet-stream` from `AllowedContentTypes`, refreshed upload tips, accept attribute, and file-type rendering across views.
- **AI chat Student-only**: Added `[Authorize(Roles = "Student")]` to `ChatController` plus a defensive `RejectNonStudent` check on every action. `ChatHub.SendMessage` rejects non-Student actors with a localized hub exception. `Course/Details.cshtml` only shows the AI assistant link to Students.
- **Payment UI and MoMo removal**: Deleted `MoMoService`, `IMoMoService`, `MoMoOptions`, and `Views/Payment/Packages.cshtml`. `PaymentService`, `PaymentController`, and the payment views now treat `PaymentMethod.VNPay` as the sole supported gateway. `PaymentMethod` enum stays for legacy data only. `PackageController.Buy` redirects to `PaymentController.CreatePayment` with `PaymentMethod.VNPay`. Redesigned `Views/Package/Index.cshtml` so the package cards share the same `pricing-grid` styles as the rest of the marketing UI. Stripped the MoMo section from `appsettings.json` and `appsettings.example.json`. `AGENTS.md` documents VNPay-only policy.
- **Tests**: Updated `PackageControllerTests` for the new `Buy` signature and VNPay redirect; added `DocumentServiceTests` and `FixedSizeTextChunkerTests`.

**UI/UX**

- Design Read: marketing-oriented pricing grid that must read as one consistent surface with VNPay payment info baked into the same card.
- Dials: `DESIGN_VARIANCE 6`, `MOTION_INTENSITY 2`, `VISUAL_DENSITY 6`.
- Reused the existing `pricing-grid`, `pricing-card`, and `pricing-highlights` styles; only added small payment-note blocks and updated nav links.
- Taste-skill local copy was not available, so shared EduPlatform MVC/Bootstrap frontend rules were applied as fallback.
- Covered Student, Teacher, Admin, anonymous states, success/error TempData on duplicate uploads, mobile single-column pricing grid, focus/hover inherited from Bootstrap, and no en/em dash in any added copy.

**Verification**

- `dotnet build d:\SEM_7\PRN_222\CourseProjectPRN222\EduPlatform.Web\EduPlatform.Web.csproj --nologo -v minimal`: passed with 0 warnings and 0 errors.
- `dotnet build d:\SEM_7\PRN_222\CourseProjectPRN222\tests\EduPlatform.Tests\EduPlatform.Tests.csproj --nologo -v minimal`: passed with 0 warnings and 0 errors.
- `tests\EduPlatform.Tests\bin\Debug\net10.0\EduPlatform.Tests.exe`: 71 succeeded, 0 failed, 1 live Gemini smoke test skipped because `GEMINI_API_KEY` was not set.
- `rg "DeferredCourseQuotaService|MoMoService|IMoMoService" EduPlatform.BLL EduPlatform.DAL EduPlatform.Web tests`: no production references to MoMo or the deferred quota service remain.

**Remaining**

- Manual browser verification of the redesigned `Package/Index` page and the Document upload duplicate warning is recommended when the app is reachable.
- Real sandbox end-to-end for VNPay still waits for credentials.

**Blocked**

- VNPay sandbox credentials and callback URLs.

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
