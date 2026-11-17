# E-Tutor (working name)

A teacher-centered learning platform for high-school students and private tutors across Jordan and
the Gulf. Students choose their country and curriculum, request access to teachers' courses, learn
from lessons and videos, take assessments and book private lessons. Teachers control their content
and approve who can access it.

> The product name is provisional. It is configured in one place per app (see [docs/setup.md](docs/setup.md#branding)).

## Repository layout

```
backend/    ASP.NET Core Web API (.NET 10), Clean Architecture + MVC controllers, EF Core + SQL Server
mobile/     Flutter app (Android first), MVC with Cubit controllers, English + Arabic (RTL)
docs/       Architecture, database design, conventions, decisions, progress
scripts/    PowerShell helpers: setup, database, run, quality gate
```

## Quick start

```powershell
./scripts/setup.ps1      # checks tools, restores packages, creates the local database
./scripts/run-api.ps1    # API on http://localhost:5258, Swagger UI at /swagger
./scripts/run-mobile.ps1 # Android emulator (uses http://10.0.2.2:5258); add -Web for Chrome
./scripts/check-all.ps1  # format checks, analyzers, builds and all tests
```

Full instructions, prerequisites and configuration: **[docs/setup.md](docs/setup.md)**.

## Documentation

| Topic | Document |
|---|---|
| Architecture and dependency rules | [docs/architecture.md](docs/architecture.md) |
| Database design (ER diagrams, constraints, booking concurrency) | [docs/database-design.md](docs/database-design.md) |
| API response envelope, errors, pagination | [docs/api-conventions.md](docs/api-conventions.md) |
| Logging and diagnostics | [docs/logging.md](docs/logging.md) |
| Security model and open risks | [docs/security.md](docs/security.md) |
| Design system and UI direction | [docs/design-direction.md](docs/design-direction.md) |
| Decision log | [docs/decisions.md](docs/decisions.md) |
| Progress, known limitations, roadmap | [docs/progress.md](docs/progress.md), [docs/known-limitations.md](docs/known-limitations.md), [docs/roadmap.md](docs/roadmap.md) |
