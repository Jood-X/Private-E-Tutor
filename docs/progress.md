# Progress

## Phase 2: Authentication & profiles (2026-11-18)

### Implemented and tested

**Backend**
* Domain: `UserProfile`, `TeacherProfile` (verification status), `StudentProfile` (curriculum, grade, optional
  stream, subjects), `RefreshToken`. Migration `UsersAndAuthentication`: ASP.NET Core Identity tables on the same
  `DbContext`, roles seeded, composite FKs tying grade/stream to the student's curriculum, unique token-hash index.
* Registration (student and teacher), sign-in with lockout, refresh-token rotation with reuse detection,
  sign-out, and own-profile read/update endpoints (`/me`, `/me/student`, `/me/teacher`) with role policies.
* Catalog consistency rules (curriculum offered in country, grade/stream/subjects belong to it) with field errors.
* Deny-by-default authorization, stricter rate limit on credential endpoints, JWT bearer in Swagger UI.
* Transactions: account + profile + first refresh token are written atomically (retried as a unit).
* `seed-dev` command for demo accounts; `setup.ps1` creates a local JWT signing key in user-secrets.

**Mobile**
* Secure token storage (Keystore), `AuthInterceptor` (one shared refresh for concurrent 401s, retry, sign-out on
  rejection, session kept when offline), app-wide `SessionCubit`, session-driven routing with splash/restore.
* Screens: welcome, sign-in, 3-step student sign-up (curriculum → grade/stream/subjects → account), teacher
  sign-up, signed-in home with profile and teacher verification banner, sign-out. EN/AR (75 strings each).
* Design system: `AppTextField` (visible label, helper, inline error, password toggle, autofill hints),
  `AppFlowScaffold`, `AppPrimaryButton` (busy state).

### Checks run

| Check | Result |
|---|---|
| `dotnet format --verify-no-changes`, `dotnet build` (warnings as errors) | clean |
| Backend unit tests | **41 passed** (+ token service, validators) |
| Backend integration tests (real API + LocalDB) | **51 passed** (+ registration rules, lockout, enumeration-safe errors, refresh rotation/reuse, forged/expired/tampered tokens, role 403s, own-profile isolation, auth rate limit) |
| `flutter analyze`, `dart format` check | clean |
| Mobile tests | **70 passed** (+ interceptor concurrency/refresh outcomes, session restore, routing rules, full sign-up/sign-in/sign-out widget flows, validation, logo layout regression) |
| Android emulator (API 35) against the local API | Welcome → sign-in → home; force-stop + relaunch restores the session from the Keystore; sign-out calls `/auth/logout` (204) |

### Found and fixed during the phase

* Refresh flow draft discarded successful refreshes (caught in review before running); rewritten.
* A network failure during token refresh would have signed users out; the interceptor now distinguishes
  "rejected" from "unreachable".
* Welcome logo stretched to full width on the device; fixed with a regression test.
* Test client did not parse enums as strings (API contract); fixed in the test envelope reader.

### Not done / not verified

* Email verification, password reset and in-app profile editing (D21, L8, L9).
* Not run on a physical device or iOS.

## Phase 1: Foundation (2026-11-14)

### Implemented and tested

**Backend** (`backend/`, .NET 10 LTS, central package versions in `Directory.Packages.props`)
* Four projects with an enforced dependency rule: Domain → Application → Infrastructure → Api.
* Catalog domain: countries, curricula (many-to-many availability), streams, grade levels, global subjects,
  grade–subject links, academic years. Names are stored as localized JSON (en/ar).
* EF Core code-first with the first migration `InitialCatalog`: explicit keys and lengths, unique
  indexes, alternate keys for future composite FKs, check constraints, RESTRICT delete by default,
  reference data seeded through migrations.
* Read-only catalog API (6 endpoints) with `Accept-Language` localization.
* Response envelope, error codes → status mapping, validation envelope, envelopes for
  404/405/429/500, global exception handler (no internal details leaked).
* Serilog structured logging with trace IDs, stable event names, request logging with route templates.
* Rate limiting, deny-by-default CORS, security headers, health check (`/health` incl. DB), Swagger UI.

**Mobile** (`mobile/`, Flutter 3.29)
* MVC structure: views → controllers (Cubit) → models (repository interfaces) ← data (API repositories).
* `ApiClient`: never throws for expected failures, parses the envelope, typed failures (network, timeout,
  validation, not found, rate limited, server, invalid response…), trace ID propagation.
* Localization: English (default) + Arabic via ARB files; language picker in Settings; app-wide RTL switch;
  `Accept-Language` sent on every request; catalog names reload when the language changes.
* Design system: color/spacing/radius/type tokens from the approved direction, theme built from tokens,
  components (`AppChoicePill`, `AppOptionTile`, skeleton/empty/error states, step progress).
* First screen: onboarding "What are you studying?" (country → curriculum) against the real API, with
  loading, empty, error-with-reference and retry states.
* Logging facade, global error hooks, controller error observer.

### Checks run (all passing)

| Check | Result |
|---|---|
| `dotnet format --verify-no-changes` | clean |
| `dotnet build` (warnings as errors, recommended analyzers) | 0 warnings, 0 errors |
| Backend unit tests | **27 passed** (domain, seed integrity, result mapping, architecture rules) |
| Backend integration tests (real API + SQL Server LocalDB, DB rebuilt from migrations) | **23 passed** (catalog incl. Arabic, 400/404/405/429/500 envelopes, no leakage, headers, health, rate limit) |
| `dart format` check, `flutter analyze` (strict casts, extra lints) | clean / no issues |
| Mobile tests | **40 passed** (API client, controllers, widget flows incl. RTL switch, WCAG contrast of tokens, translation completeness) |
| Manual: API + Flutter web in the browser at 375 px | Countries/curricula load from the API; selection works; switching to Arabic mirrors the layout and reloads names in Arabic |
| `scripts/check-all.ps1`, `scripts/db-update.ps1` | pass |

### Not yet verified

* Not run on a physical Android device or on iOS.

### Android emulator check (2026-11-17)

* `flutter build apk --debug` succeeds; the app runs on the Android 15 (API 35) emulator against the local API.
  The app log and the server log show the same trace ID for `GET /api/v1/catalog/countries`.
* Country → curriculum selection works; Continue is enabled after a choice.
* Found and fixed: button labels (and the app bar, inputs and snackbars) rendered in Roboto instead of
  Figtree, because component themes received text styles without the font family. Fixed in `AppTheme`;
  regression tests added in `test/design_system/app_theme_test.dart`.
* Found and fixed: with the API stopped, the emulator's network bridge makes connections hang until the
  10 s connect timeout, which the app reported as "the server is taking too long". Connect timeouts now map
  to the "can't reach the server" message (EN/AR); only a slow response after connecting counts as a timeout.
  Verified on the emulator: correct message with the API down, and "Try again" recovers once it's up.
  Regression test added in `test/core/api/api_client_test.dart` (mobile tests: **45 passed**).

### Found and fixed during the phase

* The design token for muted text on teal failed WCAG AA (4.20:1). The contrast test caught it; the token and mockups were fixed.
* Dio 5.11 added `transformTimeout`; the analyzer's exhaustiveness check caught the gap in error mapping.
