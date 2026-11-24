# Progress

## Phase 3b: Courses & enrollment, mobile (2026-11-18)

### Implemented and tested
* Role shells (bottom navigation, per-tab stacks): students `/s/learning`, `/s/discover`, `/s/profile`; teachers
  `/t/courses`, `/t/requests`, `/t/profile`. `redirectFor` keeps each role in its own area; Home became the Profile tab.
* Teacher: course list, create course (curriculum → grade → subject cascade), course editor (units, lessons, status
  menus, publish/archive, delete drafts), lesson editor (fields, notes, https links), enrollment requests
  (Pending/Students tabs, approve, decline with note, remove, infinite scroll).
* Student: Discover (debounced search, subject filter for the student's grade, infinite scroll, stale results dropped),
  teacher profile with "Request to join", My learning, course (outline when approved, status banner otherwise,
  withdraw), lesson (notes, links opened externally, PDF "coming soon").
* `LoadableCubit<T>`, `Paged<T>`, `ApiClient.delete`, shared status chips, dialogs and `LoadableView`.

### Checks run
| Check | Result |
|---|---|
| `flutter analyze` | no issues |
| Mobile tests | **84 passed** (+ redirect by role, discover paging/debounce/stale results, request queue, profile → request, course/lesson screens, link opening) |
| `scripts/check-all.ps1` | all passed (backend 66 unit + 72 integration) |
| Android 15 emulator vs. local API | teacher creates, fills and publishes a course; student discovers, requests; teacher approves; student opens the lesson and its link |

### Found and fixed
* Text dialogs disposed their controller while the closing animation still built the field (crash in debug). The
  dialog now owns its controller; caught by a widget test.
* Lesson links could not open on Android 11+ (package visibility): added an https `VIEW` query to the manifest.
* Dialog buttons stretched full width (theme minimum size); dialogs now use a compact action style.

## Phase 3a: Courses & enrollment, backend (2026-11-18)

### Implemented and tested
* Migration `CoursesAndEnrollments`: Courses, Units, Lessons, LessonResources, StoredFiles, Enrollments with composite
  FKs (grade/year in the course's curriculum, subject taught at the grade, lesson's course = its unit's course),
  CHECK constraints (status ranges, resource type ↔ source), filtered unique index for active enrollments, row versions.
* Teacher content: courses (create, edit, status rules), units and lessons (create, edit, status, reorder, delete drafts),
  resources (HTTPS links, PDF upload with signature check), outline with counts.
* Enrollment: request (409 on duplicates, even concurrent), withdraw, teacher queue (paginated), approve/reject/revoke
  with notes; transitions are pure, unit-tested rules.
* Discovery: verified, accepting teachers filtered by curriculum/grade/subject/search (paginated, no N+1); teacher
  profile with published courses and the caller's enrollment status.
* Student access: course summary vs. outline, lesson detail and document download only with an approved enrollment.
* Shared pagination contract, 409 for concurrent edits, local file storage with path-traversal guard.

### Checks run
| Check | Result |
|---|---|
| `dotnet format`, `dotnet build` (warnings as errors) | clean |
| Backend unit tests | **66 passed** (+ enrollment transitions, course status rules, link rules) |
| Backend integration tests | **72 passed** (+ cross-teacher 404s, student → teacher 403s, approval gating, revoke/withdraw, concurrent duplicate requests, discovery filters/verification, pagination, PDF validation and download authorization) |

### Found and fixed
* "My enrollments" returned 500: ordering after projecting into a record isn't translatable by EF Core. Caught by an
  integration test; the query now orders before projecting.
* The decision endpoint's response could fail for courses with more than 50 enrollments (it re-read a page); replaced
  with a single-row projection.

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
