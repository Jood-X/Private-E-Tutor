# API Conventions

## Base path and versioning

All endpoints live under `/api/v1/…`. A breaking change ships under a new version prefix.
Interactive documentation: **Swagger UI at `/swagger`** (Development), OpenAPI document at `/openapi/v1.json`.

## Response envelope

Every JSON endpoint returns the same shape, for success and failure:

```json
{
  "success": true,
  "message": null,
  "data": [ { "isoCode": "JO", "name": "Jordan", "defaultTimeZoneId": "Asia/Amman" } ],
  "errors": null,
  "traceId": "9e936a89ed3888539219ec49f1fbe9a9"
}
```

```json
{
  "success": false,
  "message": "Country not found.",
  "data": null,
  "errors": [ { "code": "catalog.country_not_found", "message": "Country not found.", "field": null } ],
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

| Field | Meaning |
|---|---|
| `success` | `true` only for 2xx results. |
| `message` | Optional human-readable summary (English fallback). Clients must not display it directly. |
| `data` | The payload; `null` on failure. |
| `errors` | `null` on success; otherwise one or more `{ code, message, field }`. |
| `traceId` | W3C trace ID of the request. Also sent as the `X-Trace-Id` header and written on every server log line. Users can quote it as "Reference". |

**Exceptions (no envelope):** `/health` (plain `Healthy`/`Unhealthy`), `/openapi/*`, and future file
downloads/streams, which use standard HTTP semantics.

Implementation: controllers inherit `ApiControllerBase` and return `OkResponse(...)` or `FromResult(...)`.
No controller formats responses itself. Middleware produces the same envelope for 404/405 (unknown
routes), 429 (rate limit), validation failures and unhandled exceptions.

## Error codes and status codes

`errors[].code` is a stable, machine-readable key (`area.reason`). **Clients translate codes into
localized messages**; codes are never renamed once published. They're defined in
`backend/src/ETutor.Application/Common/ErrorCodes.cs`.

| Status | When | Example codes |
|---|---|---|
| 400 | Invalid input (one error per field, `field` in camelCase) | `common.validation_failed`, `catalog.invalid_country_code` |
| 401 | Not authenticated, bad credentials, expired session | `common.unauthorized`, `auth.invalid_credentials`, `auth.locked_out`, `auth.invalid_refresh_token` |
| 403 | Authenticated but not allowed | `common.forbidden` |
| 404 | Not found, **or exists but belongs to someone else** (prevents ID probing) | `common.not_found`, `catalog.country_not_found` |
| 405 | Wrong HTTP method | `common.method_not_allowed` |
| 409 | Conflict / concurrent modification | `auth.email_taken`, `enrollment.already_requested`, `common.concurrency_conflict` |
| 422 | Valid input that breaks a business rule | `course.no_published_lesson`, `video.not_found`, `video.not_embeddable`, `video.unavailable` |
| 503 | A dependency is temporarily unreachable; retry later | `video.provider_unavailable` |
| 429 | Rate limited (`Retry-After` header set) | `common.rate_limited` |
| 500 | Unexpected failure. Generic message only; no exception text, stack trace or SQL. | `common.unexpected` |

## Conventions

* JSON uses camelCase; enums are serialized as strings; dates are ISO-8601 (`2026-08-23`); instants are UTC with `Z`.
* Send `Accept-Language: en` or `ar`. Catalog names and fallback messages are localized; anything else falls back to English.
* Collections that can grow are paginated as `data: { items, page, pageSize, totalCount, totalPages }`.
  Query `page` (from 1) and `pageSize` (default 20, max 50); out-of-range values are clamped, not rejected.
  The catalog is small and bounded, so it isn't paginated.
* Entities are never serialized directly; every endpoint returns DTOs from `ETutor.Application`.

## Current endpoints (Phase 1)

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/catalog/countries` | public | Active countries |
| GET | `/api/v1/catalog/countries/{isoCode}/curricula` | public | Curricula offered in a country |
| GET | `/api/v1/catalog/curricula/{id}/streams` | public | Streams of a curriculum (may be empty) |
| GET | `/api/v1/catalog/curricula/{id}/grade-levels` | public | Grade levels, ordered |
| GET | `/api/v1/catalog/curricula/{id}/academic-years` | public | Academic years, newest first |
| GET | `/api/v1/catalog/grade-levels/{id}/subjects` | public | Subjects taught at a grade level |
| GET | `/health` | public | Liveness + database connectivity |
| POST | `/api/v1/auth/register/student` | public, auth rate limit | Create a student account + study plan; returns tokens and profile (201) |
| POST | `/api/v1/auth/register/teacher` | public, auth rate limit | Create a teacher account (verification Pending); returns tokens and profile (201) |
| POST | `/api/v1/auth/login` | public, auth rate limit | Email + password; returns tokens and profile |
| POST | `/api/v1/auth/refresh` | public, auth rate limit | Exchange a refresh token (single-use) for a new pair |
| POST | `/api/v1/auth/logout` | public, auth rate limit | Revoke the session that owns the refresh token (204, also for unknown tokens) |
| GET | `/api/v1/me` | signed in | The caller's own profile (`MeDto`) |
| PUT | `/api/v1/me` | signed in | Update name, language, time zone, country |
| PUT | `/api/v1/me/student` | Student role | Replace the study plan (curriculum, grade, stream, subjects) |
| PUT | `/api/v1/me/teacher` | Teacher role | Update headline, bio, accepting-students flag |

| GET | `/api/v1/teacher/courses` | Teacher | My courses with unit/lesson/request/student counts |
| POST | `/api/v1/teacher/courses` | Teacher | Create a draft course (201) |
| GET/PUT | `/api/v1/teacher/courses/{id}` | Teacher (owner) | Full outline / edit title, description, language |
| PUT | `/api/v1/teacher/courses/{id}/status` | Teacher (owner) | Draft / Published / Archived (rules: D22) |
| POST | `/api/v1/teacher/courses/{id}/units` | Teacher (owner) | Add a unit |
| PUT, DELETE | `/api/v1/teacher/units/{id}` (+ `/status`, `/position`) | Teacher (owner) | Edit, change status, reorder, delete drafts |
| POST | `/api/v1/teacher/units/{id}/lessons` | Teacher (owner) | Add a lesson |
| GET, PUT, DELETE | `/api/v1/teacher/lessons/{id}` (+ `/status`, `/position`) | Teacher (owner) | Lesson detail, edit, status, reorder, delete drafts |
| POST | `/api/v1/teacher/lessons/{id}/resources/link` | Teacher (owner) | Add an HTTPS link |
| POST | `/api/v1/teacher/lessons/{id}/resources/video` | Teacher (owner) | Link a YouTube video (`url`: share link or ID, optional `title`, `isRequired`); checked with the provider first |
| POST | `/api/v1/teacher/lessons/{id}/resources/document` | Teacher (owner) | Upload a PDF (multipart: `file`, `title`, `isRequired`; max 20 MB) |
| DELETE | `/api/v1/teacher/resources/{id}` | Teacher (owner) | Remove a resource (and its file) |
| GET | `/api/v1/teacher/enrollments?status=&courseId=&page=&pageSize=` | Teacher | Request queue and students (paginated) |
| POST | `/api/v1/teacher/enrollments/{id}/approve` · `/reject` · `/revoke` | Teacher (owner) | Decide, with an optional note |
| GET | `/api/v1/teachers?curriculumId=&gradeLevelId=&subjectId=&search=&page=&pageSize=` | Student | Discover verified teachers (paginated) |
| GET | `/api/v1/teachers/{id}` | Student | Teacher profile, published courses and my enrollment status |
| POST | `/api/v1/courses/{id}/enrollments` | Student | Request access (201; 409 if already requested) |
| GET | `/api/v1/learning/enrollments` | Student | My enrollments |
| POST | `/api/v1/learning/enrollments/{id}/withdraw` | Student (owner) | Withdraw a request or enrollment |
| GET | `/api/v1/learning/courses/{id}` | Student | Course summary; outline only when approved |
| GET | `/api/v1/learning/lessons/{id}` | Student (approved) | Lesson notes and resources |
| GET | `/api/v1/resources/{id}/file` | Owner teacher or approved student | **File response** (not the envelope) |
| GET | `/api/v1/resources/{id}/playback` | Owner teacher or approved student | Provider and video ID for the in-app player (`no-store`) |

### Authentication

Send `Authorization: Bearer <accessToken>`. Access tokens expire after 15 minutes. Exchange the refresh token
at `/auth/refresh` (each refresh token works **once**; a replayed one signs the session out everywhere it was
used). Authorization is deny-by-default: without a valid token every non-public endpoint, and any unknown
route, answers **401** with `common.unauthorized`. A valid token without the right role gets **403**.
