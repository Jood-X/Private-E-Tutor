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

## Planned (by phase)

| Phase | Control |
|---|---|
| 2 | Identity password hashing and lockout; JWT (15 min) + rotating refresh tokens stored as hashes with reuse detection; tokens in Android Keystore via `flutter_secure_storage`; strict rate limits on `/auth/*`; role policies. |
| 3 | Resource-based authorization helpers (`teacher owns course`, `student has approved enrollment`). Cross-user access tests for every protected endpoint. |
| 3–4 | File uploads: size/type allow-list, content sniffing, random storage keys, authorized download endpoint. |
| 4 | Video playback details only for authorized users; YouTube API key server-side only. See the limitations below. |
| 5 | Answer keys never in student DTOs; server-side scoring and timers. |
| 6 | Booking ownership checks; DB-enforced slot reservations against double booking. |

## Open risks and limitations

* **Rate limiting behind a proxy:** client IP partitioning needs forwarded-headers configuration when deployed
  behind a reverse proxy or load balancer. Otherwise all clients share one bucket.
* **YouTube content protection (Phase 4):** unlisted YouTube IDs can be extracted and shared. YouTube offers no
  signed URLs or DRM for ordinary channels. See `docs/decisions.md` D3.
* **Minors' data:** students are typically under 18. Parental-consent and data-protection requirements per
  country need legal review before launch (decision D10).
