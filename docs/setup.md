# Development Setup

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | **10.0.x** (LTS) | `dotnet --list-sdks` must show a 10.0 SDK. |
| SQL Server | LocalDB (ships with Visual Studio) or any SQL Server 2019+ | Default connection uses `(localdb)\MSSQLLocalDB`. |
| Flutter | 3.29+ (stable) | `flutter doctor` must be clean for Android. |
| Android SDK | API 35 | Accept licenses: `flutter doctor --android-licenses`. |
| PowerShell | 5.1+ | For the scripts in `scripts/`. Every script step is also listed below as a plain command. |

The EF Core CLI is a **repo-local tool** (`backend/.config/dotnet-tools.json`); `dotnet tool restore` installs the pinned version.

## First-time setup

```powershell
./scripts/setup.ps1
```

This is equivalent to:

```powershell
cd backend
dotnet tool restore
dotnet restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/ETutor.Infrastructure --startup-project src/ETutor.Api
cd ../mobile
flutter pub get          # also generates lib/l10n/generated (localizations)
```

## Database

The schema is defined in C# (`backend/src/ETutor.Domain` entities + `ETutor.Infrastructure/Persistence/Configurations`)
and **EF Core migrations are the single source of truth** (`ETutor.Infrastructure/Persistence/Migrations`).
Reference data (countries, curricula, grades, subjects, academic years) is part of the migrations.

| Task | Command |
|---|---|
| Create or upgrade the local database | `./scripts/db-update.ps1` |
| Add a migration after changing the model | `./scripts/add-migration.ps1 -Name AddEnrollments` (review the generated file!) |
| Drop and recreate the local database (**deletes all data**) | `./scripts/db-reset.ps1` (asks for confirmation) |
| Generate a SQL script for a DBA/production | `dotnet ef migrations script --idempotent --project src/ETutor.Infrastructure --startup-project src/ETutor.Api -o migrate.sql` (from `backend/`) |

Development/demo data (teachers, students, courses) will be added by a `seed-dev` command when those tables exist (Phase 2+).

## Configuration

Backend settings live in `backend/src/ETutor.Api/appsettings.json` (safe defaults, no secrets) and
`appsettings.Development.json` (LocalDB connection, verbose logs). Override anything with environment
variables using `__` as the separator.

| Setting | Env variable | Default | Purpose |
|---|---|---|---|
| `ConnectionStrings:Database` | `ConnectionStrings__Database` | LocalDB in Development, **empty in production** (startup fails fast) | SQL Server connection |
| `Branding:ProductName` | `Branding__ProductName` | `E-Tutor` | Name shown in Swagger |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` … | none (Development: `http://localhost:8686`) | Browser origins allowed to call the API. Native apps are unaffected. |
| `RateLimiting:PermitLimit` / `WindowSeconds` | `RateLimiting__PermitLimit` … | 120 per 60 s per client IP | Abuse protection |
| `Serilog:*` | `Serilog__MinimumLevel__Default` … | JSON console (prod), readable console + daily file (dev) | Logging |

Secrets (connection strings with passwords, future JWT keys, YouTube API key) must **never** be committed.
Use `dotnet user-secrets` locally (`dotnet user-secrets set "ConnectionStrings:Database" "..." --project src/ETutor.Api`)
or environment variables on servers. `.gitignore` blocks `appsettings.Production.json`, `.env` and key files.

### Branding

* Backend: `Branding:ProductName`.
* Mobile: `mobile/lib/core/config/branding.dart`, or `--dart-define=PRODUCT_NAME="New Name"` at build time.
  The Android launcher label is in `android/app/src/main/AndroidManifest.xml` (`android:label`).

## Running

```powershell
./scripts/run-api.ps1                      # http://localhost:5258, Swagger UI: http://localhost:5258/swagger
```

Mobile (from `mobile/`):

| Target | Command |
|---|---|
| Android emulator | `flutter run` (the default API URL `http://10.0.2.2:5258` reaches the host machine) |
| Physical Android device | `flutter run --dart-define=API_BASE_URL=http://<your-PC-LAN-IP>:5258` and run the API with `--urls http://0.0.0.0:5258` |
| Web (development preview) | `flutter run -d chrome --web-port 8686 --dart-define=API_BASE_URL=http://localhost:5258` |

Plain HTTP is allowed **only in Android debug builds** (`android/app/src/debug/AndroidManifest.xml`). Release builds require HTTPS.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `Error: No pubspec.yaml file found` | Flutter commands must run inside `mobile/`: `cd mobile` first. |
| Gradle: `Could not GET 'https://dl.google.com/...'` / `No such host is known` | The machine lost network/DNS while Gradle was downloading build plugins. Check the connection (`Resolve-DnsName dl.google.com`) and run `flutter run` again. The first Android build downloads several hundred MB (Gradle, plugins, NDK) and can take 10+ minutes. Later builds are fast. |
| Warning: `shared_preferences_android requires Android NDK 27.0.12077973` | Harmless: NDKs are backward compatible and the app has no native code. Optionally set `ndkVersion = "27.0.12077973"` in `android/app/build.gradle.kts` (triggers a ~1 GB NDK download). |
| App shows loading placeholders for several seconds on first launch | The first API call wakes up LocalDB (~4 s). Later calls are fast. |
| App shows "You appear to be offline" | The API isn't running, or (physical device) `API_BASE_URL` doesn't point at your PC's LAN IP. |

## Quality gate

```powershell
./scripts/check-all.ps1
```

Runs: `dotnet format --verify-no-changes`, `dotnet build` (warnings are errors), backend unit + integration
tests, `flutter pub get`, `dart format` check, `flutter analyze`, `flutter test`.

Backend integration tests host the real API against SQL Server. They **drop and recreate** the database
`ETutor_IntegrationTests` on every run (never your dev database). Point them elsewhere with
`$env:ETUTOR_TEST_DB = "<connection string>"`.
