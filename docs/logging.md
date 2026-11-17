# Logging and Diagnostics

## Correlating a problem end to end

1. Every API response carries a **trace ID** (`traceId` in the body and the `X-Trace-Id` header).
2. The mobile app logs it with each request (`GET /api/v1/... -> 404 in 35ms trace=…`) and shows it
   to users as "Reference: …" on error screens.
3. Every server log line for that request carries the same `TraceId`. Search for it to see the full story.

## Backend (Serilog)

| Environment | Output |
|---|---|
| Development | Readable console (`[time LVL] message <SourceContext> trace=…`) + daily JSON file `src/ETutor.Api/logs/etutor-*.log` (7 days kept, git-ignored). EF SQL commands are logged at Information. |
| Production | Compact JSON to stdout (for a log collector). Microsoft/EF noise at Warning. |

* **Request log:** one line per request with method, path, status, elapsed time, route template
  (`RouteTemplate`) and `TraceId`. 5xx requests are logged as Error.
* **Event names** are stable and source-generated (`backend/src/ETutor.Api/Logging/LogMessages.cs`):

| EventId | Name | Level |
|---|---|---|
| 1000 | `request.unhandled_exception` | Error (with exception) |
| 1001 | `request.validation_failed` | Information (field names only) |
| 1002 | `request.failed` | Information (error code + type) |
| 1003 | `request.rate_limited` | Warning |
| 1004 | `request.bad_request` | Information |

New areas add their own ranges (e.g. 2000 auth, 3000 enrollment, 4000 video provider, 5000 booking).

**Never log:** passwords, tokens (access/refresh), API keys, request/response bodies, full query strings,
or personal data beyond opaque IDs. The request logger records the path without the query string.

## Mobile

* `AppLogger` (`lib/core/logging/app_logger.dart`) wraps `package:logging`. Debug builds log everything to the
  console; **release builds keep warnings and above only**.
* HTTP: one line per request (method, path without query, status, duration, trace ID). Headers and bodies are never logged.
* Global hooks: `FlutterError.onError`, `PlatformDispatcher.onError` and `AppBlocObserver` (controller errors).
  Severe logs go to `ErrorReporter`. Today that's console-only; a crash-reporting service can implement the
  interface later without changing callers.
* Users see localized messages (`core/errors/failure_messages.dart`), never raw server or exception text.
