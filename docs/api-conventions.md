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
| 401 | Not authenticated | `common.unauthorized` |
| 403 | Authenticated but not allowed | `common.forbidden` |
| 404 | Not found, **or exists but belongs to someone else** (prevents ID probing) | `common.not_found`, `catalog.country_not_found` |
| 405 | Wrong HTTP method | `common.method_not_allowed` |
| 409 | Conflict / concurrent modification | (booking, enrollment: later phases) |
| 422 | Valid input that breaks a business rule | (later phases) |
| 429 | Rate limited (`Retry-After` header set) | `common.rate_limited` |
| 500 | Unexpected failure. Generic message only; no exception text, stack trace or SQL. | `common.unexpected` |

## Conventions

* JSON uses camelCase; enums are serialized as strings; dates are ISO-8601 (`2026-08-23`); instants are UTC with `Z`.
* Send `Accept-Language: en` or `ar`. Catalog names and fallback messages are localized; anything else falls back to English.
* Collections that can grow are paginated (introduced with the first such endpoint) as
  `data: { items, page, pageSize, totalCount, totalPages }`, with `page` from 1 and a server-capped `pageSize`.
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
