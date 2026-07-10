# Security Review

Review date: 2026-07-02

Scope: Huy-owned foundation, Course module, Gmail service, MVC layout, and project configuration.

## Passed Checks

- [x] Cookie authentication uses a `__Host-` cookie, `HttpOnly`, `Secure`, and `SameSite=Lax`.
- [x] Authentication middleware runs before authorization middleware.
- [x] Role policies use standard ASP.NET Core authorization.
- [x] State-changing MVC actions use automatic anti-forgery validation.
- [x] Anti-forgery cookie is secure and `HttpOnly`.
- [x] Controllers call BLL services and do not reference DAL.
- [x] Web has no direct project reference to DAL.
- [x] Course authorization is enforced again inside BLL, not only in controllers.
- [x] EF Core parameterizes course search queries.
- [x] Course and enrollment passwords are hashed with BCrypt.
- [x] Razor output encoding remains enabled.
- [x] Email templates HTML-encode dynamic names, course titles, package names, and references.
- [x] MailKit uses STARTTLS for Gmail.
- [x] Configuration files contain blank placeholders only.
- [x] Production startup does not run database migrations.
- [x] Security headers include CSP, frame denial, MIME sniffing protection, and referrer policy.
- [x] Payment schema contains unique internal and gateway references for later idempotent processing.
- [x] Error pages do not expose exception details in production.

## Pending Checks

- [ ] AccountController login, logout, registration, password reset, and claims issuance. Owner: Nguyên.
- [ ] Subscription quota enforcement concrete implementation. Owner: Nguyên.
- [ ] Document upload file validation, storage authorization, and malware controls. Owner: Nhân.
- [ ] VNPay signature validation and callback idempotency. Owner: Nhân.
- [ ] Gemini prompt injection controls, output encoding, and API timeout/retry behavior. Owners: Nhân and Bảo.
- [ ] SignalR authorization and per-user session isolation. Owner: Bảo.
- [ ] Full end-to-end authorization test after all modules are integrated.
- [ ] Production secret store and credential rotation procedure.
- [ ] CSP review when third-party scripts, charts, or payment widgets are introduced.

## Known Limitation

Course quota now runs through `SubscriptionCourseQuotaService`. The next Package/Subscription pass should verify active subscription, expired subscription, missing subscription, under-limit, exact-limit, Admin bypass, and concurrent creation behavior against real database state.
