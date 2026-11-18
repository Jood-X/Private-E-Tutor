# Security

Living document: each phase updates the "Implemented" and "Open risks" sections.

## Principles

* The API is the only authority. Mobile UI restrictions are convenience, never security.
* Ownership is enforced **inside queries** (the row is filtered by the current user's ownership or enrollment),
  so a foreign ID returns 404 rather than leaking existence.
* Established framework components only (ASP.NET Core Identity, JWT bearer, rate limiter). No custom crypto.
* Secrets come from environment variables or `dotnet user-secrets`, never from the repository.

## Implemented (Phase 1)

| Control | Where |
|---|---|
| Generic 500 responses (no exception text, stack trace or SQL) | `GlobalExceptionHandler`. Covered by an integration test. |
| Input validation with field-level 400s | `ValidationResponseFactory`, service-level checks |
| Rate limiting per client IP (fixed window), 429 + `Retry-After` | `ServiceCollectionExtensions.AddApiRateLimiting`. Covered by an integration test. |
| CORS deny-by-default; explicit allow-list per environment | `Cors:AllowedOrigins` |
| Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) | Response-header middleware. Covered by a test. |
| HSTS + HTTPS redirection outside Development/Testing | `UseApiPipeline` |
| Parameterized SQL only (EF Core LINQ; no raw SQL) | All data access |
| Fail-fast when the connection string is missing | `Infrastructure.DependencyInjection` |
| Android: cleartext HTTP only in debug builds | `android/app/src/debug/AndroidManifest.xml` |
| No secrets or bodies in logs | `docs/logging.md` |
| Architecture tests prevent layers from bypassing the Application layer | `LayerDependencyTests` |

## Implemented (Phase 2)

| Control | Where | Tested by |
|---|---|---|
| Password hashing (PBKDF2 via ASP.NET Core Identity); policy 8+ chars with letter and digit | `IdentityService`, Identity options | `RegistrationTests` |
| Lockout after 5 failed sign-ins for 15 minutes | Identity lockout | `SessionTests.Login_LocksTheAccount…` |
| Same error and similar timing for unknown email vs wrong password | `IdentityService.CheckCredentialsAsync` (dummy hash) | `SessionTests.Login_UnknownEmailAndWrongPassword…` |
| JWT validation: issuer, audience, lifetime, HMAC-SHA256 only, 30 s clock skew | `AuthenticationSetup` | forged / expired / tampered token tests |
| Signing key ≥ 256 bits from user-secrets or environment; startup fails without it | `JwtOptions` validation | `TokenServiceTests` |
| Refresh tokens: random 256-bit, stored hashed, single-use, rotation, reuse revokes the family, atomic "use once" update | `SessionService`, `RefreshTokens` table | `SessionTests.Refresh_…`, `Logout_…` |
| Deny-by-default authorization; role policies for student/teacher endpoints | Fallback policy, `Policies` | `ProfileTests…IsForbidden`, `Me_WithoutToken…` |
| Users can only read/update their own profile (ID comes from the token, never the request) | `ProfileService` | `UpdateProfile_ChangesOnlyTheCallersOwnProfile` |
| Stricter rate limit on credential endpoints (10/min per IP by default) | `auth` rate-limit policy | `AuthRateLimitingTests` |
| Registration is atomic (account + profile + first token in one transaction) | `ExecuteInTransactionAsync` | `RegisterStudent_FailedRegistration_DoesNotCreateAccount` |
| Security events logged without emails, passwords or tokens | `UserLog` (EventIds 2000–2005) | review |
| Mobile: tokens in Android Keystore (flutter_secure_storage), never in logs (`AuthTokens.toString` redacted) | `SecureTokenStore` | `auth_interceptor_test` |
| Mobile: one shared refresh for concurrent 401s (single-use tokens); network failure keeps the session | `AuthInterceptor` | `auth_interceptor_test` |

## Planned (by phase)

| Phase | Control |
|---|---|
| 3 | Resource-based authorization helpers (`teacher owns course`, `student has approved enrollment`). Cross-user access tests for every protected endpoint. |
| 3–4 | File uploads: size/type allow-list, content sniffing, random storage keys, authorized download endpoint. |
| 4 | Video playback details only for authorized users; YouTube API key server-side only. See the limitations below. |
| 5 | Answer keys never in student DTOs; server-side scoring and timers. |
| 6 | Booking ownership checks; DB-enforced slot reservations against double booking. |

## Open risks and limitations

* **No email verification or password reset yet (D21):** anyone can register with an email they don't own, and
  a user who forgets their password cannot recover the account. Needs an email provider.
* **Lockout reveals that an account exists:** a locked-out response (`auth.locked_out`) is only returned for real
  accounts. Accepted trade-off for a clearer user message; revisit if enumeration becomes a concern.
* **Lockout can be used to annoy a known user** (5 wrong passwords lock them out for 15 minutes). The per-IP
  auth rate limit slows this down; per-account throttling or CAPTCHA can be added later.
* **Single-use refresh tokens and parallel clients:** two app instances sharing one stored token would sign each
  other out. The app serializes refreshes; other clients must do the same.
* **Rate limiting behind a proxy:** client IP partitioning needs forwarded-headers configuration when deployed
  behind a reverse proxy or load balancer. Otherwise all clients share one bucket.
* **YouTube content protection (Phase 4):** unlisted YouTube IDs can be extracted and shared. YouTube offers no
  signed URLs or DRM for ordinary channels. See `docs/decisions.md` D3.
* **Minors' data:** students are typically under 18. Parental-consent and data-protection requirements per
  country need legal review before launch (decision D10).
