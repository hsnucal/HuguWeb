# Local Development

This document covers running HuGuWeb on a developer machine. It does not repeat architecture ADRs.

## Prerequisites

- Windows-first workflow (PowerShell)
- .NET SDK 10
- Node.js 24 LTS and npm (`npm.cmd` on Windows)
- PostgreSQL 18 tools (`pg_isready` / `pg_ctl`) and an existing HuGuWeb development cluster
- Cursor / VS Code with the Cursor C# debugger (`anysphere.csharp`) for F5 API breakpoints. F5 uses `coreclr` and launches the Debug `HuGuWeb.Api.dll` (not the apphost `.exe`).

PostgreSQL is required for login, session cookies, and `/health/ready`. The API can start without it; `/health` still reports process liveness.

Do not commit secrets. Connection strings and passwords stay in .NET User Secrets or local environment variables.

## Preferred workflow: F5

Open the **repository root** in Cursor / VS Code (not `src/frontend/web` alone).

1. Press **F5**.
2. Select **HuGuWeb Development**.

Expected sequence:

1. PostgreSQL is checked on `localhost:5432` with `pg_isready`. If it is already accepting connections, it is **not** restarted. If it is not ready, the existing HuGuWeb development cluster is started (never a new cluster).
2. Vite starts as a VS Code background task from `src/frontend/web` using `npm.cmd run dev`, or is reused if `http://localhost:5173` already returns 200. The long-running Vite process is not treated as task failure.
3. F5 waits until `http://localhost:5173` returns 200. If Vite does not become ready, the API debugger is not started.
4. The API project is built if needed.
5. The ASP.NET Core API starts **under the C# `coreclr` debugger** at `http://localhost:5116`. The debuggee is `src/backend/HuGuWeb.Api/bin/Debug/net10.0/HuGuWeb.Api.dll` (current Debug build + matching PDB). Cursor's C# debugger (`netcoredbg`) launches `"dotnet" "<that-dll>"`. That is **not** `dotnet run --launch-profile http`, so `launchSettings.json` `applicationUrl` is **not** applied unless `ASPNETCORE_URLS=http://localhost:5116` is set in `.vscode/launch.json`. Without that env var, Kestrel listens on the default `http://localhost:5000` and Vite's proxy to `5116` fails with `ECONNREFUSED`. Environment also sets `Development` and `ApplicationName` `HuGuWeb.Api` (existing User Secrets). Do not point `program` at `HuGuWeb.Api.exe`; that launches `dotnet <apphost>` and leaves breakpoints unbound. `.vscode/launch.json` does not contain passwords or connection strings. `type: "dotnet"` / `projectPath` is not used because the C# Dev Kit service broker is not part of this Cursor C# install.
6. A small helper waits until `http://localhost:5116/health` returns 200, then opens Chrome at `http://localhost:5173`. It does not start another debug session. If `/health` never becomes 200, Chrome is not opened.

Opening Chrome before `/health` is 200 causes `/api/auth/csrf` to fail and the login form to show a generic sign-in error. If PostgreSQL or Vite fails, F5 stops before the debugger. Do not use Debug Anyway as the normal path.

| Surface | Address | Opened in Chrome? |
| --- | --- | --- |
| Frontend (Vite SPA) | http://localhost:5173 | **Yes** — this is the F5 browser target |
| API | http://localhost:5116 | No. Use `/health` and `/health/ready` directly if needed |
| PostgreSQL | localhost:5432 | **No.** This is a database port, not an HTTP URL |

The first F5 in a window, choose **HuGuWeb Development**. Later F5 presses reuse that selection.

Stopping the debug session stops the API debugger and Chrome. The Vite terminal can be stopped with that terminal's kill control. PostgreSQL is left running. F5 does not run `taskkill /IM node.exe`, `taskkill /IM dotnet.exe`, or equivalent broad process kills.

`.vscode/launch.json` and `.vscode/tasks.json` are repository-owned F5 configuration. They do not contain passwords, connection strings, tokens, or User Secrets.

The PostgreSQL pre-launch task starts a **process-local** PowerShell with `-ExecutionPolicy Bypass -File dev.ps1 -EnsurePostgres`. That override applies only to the launched `powershell.exe` process. Machine and user execution policy are not changed. Do not run `Set-ExecutionPolicy`.

Do **not** open `http://localhost:5432` in Chrome. Port 5432 is PostgreSQL, not HTTP.

## CLI fallback: `.\dev.ps1`

F5 is the preferred daily workflow. The launcher remains supported when the IDE debugger is not used.

From the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\dev.ps1
```

`Bypass` applies only to that process. Do not run `Set-ExecutionPolicy`. If a terminal already allows local scripts, `.\dev.ps1` is equivalent.

To stop only launcher-owned API/frontend processes:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\dev-stop.ps1
```

The launcher:

1. Checks that .NET 10, Node 24, `npm.cmd`, and PostgreSQL 18 tools are present. It does **not** install software and does **not** change PowerShell execution policy.
2. Checks `localhost:5432` with `pg_isready` when available. If PostgreSQL is already ready, it is **not** restarted.
3. If PostgreSQL is not ready, starts the **existing** HuGuWeb development cluster. Discovery order: `PATH`, `C:\Program Files\PostgreSQL\18\bin`, then data under `%LOCALAPPDATA%\HuGuWeb\PostgreSQL\data` or `C:\Program Files\PostgreSQL\18\data`. It does not create a cluster, reset data, change passwords, or recreate `huguweb_dev`.
4. Starts the ASP.NET API with the existing Development `http` launch profile if `/health` is not already 200.
5. Waits for `/health` and `/health/ready`.
6. Starts the Vite app from `src/frontend/web` with `npm.cmd run dev` if the frontend URL is not already reachable. It uses `npm.cmd` so `npm.ps1` execution-policy issues are avoided.

When startup succeeds, it prints:

- Frontend URL (from Vite config, currently `http://localhost:5173`)
- API URL (from launch settings, currently `http://localhost:5116`)
- PostgreSQL `localhost:5432` (not an HTTP URL; not opened in Chrome)

It does not print passwords, connection strings, User Secrets, cookies, or tokens.

`.\dev.ps1 -EnsurePostgres` (or the same file with process-local `-ExecutionPolicy Bypass`) only performs the PostgreSQL check/start used by the F5 pre-launch task. `-StartVite` and `-WaitFrontend` start/wait for Vite before the API debugger. `-StartChromeHealthWatcher` / `-OpenChromeWhenHealthy` open Chrome only after `/health` is 200.

### Stopping

- PostgreSQL is left running.
- API and frontend run in separate consoles titled `HuGuWeb API` and `HuGuWeb Frontend`. Close those windows to stop them.
- `.\dev-stop.ps1` stops **only** processes recorded by the launcher, after checking that the command line still looks like HuGuWeb API/frontend. It never runs `taskkill /IM node.exe` or `taskkill /IM dotnet.exe`. F5 sessions are stopped from the IDE; they do not depend on `dev-stop.ps1`.
- Closing the launcher window does not kill unrelated processes.

### Common failures

| Symptom | What to check |
| --- | --- |
| Missing .NET / Node / npm / PostgreSQL tools | Install the expected major version and reopen the terminal (and Cursor, so PATH is picked up). The launcher will not install them. |
| PostgreSQL not ready and no data directory found | The existing cluster must already exist. Follow the manual PostgreSQL notes below. The launcher will not initialize a new cluster. |
| `/health` never becomes 200 | Read the `HuGuWeb API` console or the F5 API terminal. |
| `/health/ready` fails | Database connection or pending migrations. Configure User Secrets locally; do not put passwords in `dev.ps1` or `.vscode`. |
| Frontend never becomes ready | Run `npm.cmd install` in `src/frontend/web` if `node_modules` is missing. Close any leftover Vite terminal first so port 5173 is free. Read the Vite / `HuGuWeb Frontend` console. |
| `running scripts is disabled` / PSSecurityException | Do not run `Set-ExecutionPolicy`. F5 already uses process-local `-ExecutionPolicy Bypass`. For CLI, use `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\dev.ps1`. |
| `npm.ps1` is blocked | Use `npm.cmd` (F5 and the launcher already do). Do not weaken machine execution policy. |
| Chrome opened the wrong port | F5 must use **HuGuWeb Development**. The browser target is `http://localhost:5173`, never `8080`, `5116`, or `5432`. |

CLI frontend start previously passed a quoted `C:\Program Files\nodejs\npm.cmd` path into `Start-Process`. PowerShell then wrapped that already-quoted argument again, so `cmd.exe /k` could open a console without starting Vite while a leftover listener on port 5173 still made the readiness check succeed. The launcher now runs `npm.cmd run dev` by PATH name with a single `/k` command string. F5 does not use those extra consoles; it runs `npm.cmd run dev` as a VS Code background task.

## Manual PostgreSQL notes

Local PostgreSQL 18 typically uses:

- Binaries: `C:\Program Files\PostgreSQL\18`
- Data directory: `%LOCALAPPDATA%\HuGuWeb\PostgreSQL\data` or `C:\Program Files\PostgreSQL\18\data`
- Database: `huguweb_dev`
- Application role: `huguweb` (not a superuser)
- Connection string and passwords: .NET User Secrets / local environment only

The Windows installer cluster init can fail when the OS locale name contains non-ASCII characters. A cluster may be initialized with `locale=C` / UTF8. If a Windows service is not registered, start/stop as the installing user:

```bash
"C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" start -D "C:\Program Files\PostgreSQL\18\data"
"C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" stop -D "C:\Program Files\PostgreSQL\18\data"
```

Set the connection string locally (never commit the password):

```bash
dotnet user-secrets set "ConnectionStrings:IdentityDatabase" "Host=localhost;Port=5432;Database=huguweb_dev;Username=huguweb;Password=<local-app-password>" --project src/backend/HuGuWeb.Api
```

## Repository layout

- `src/backend/HuGuWeb.Api` — ASP.NET Core API host
- `src/backend/modules/HuGuWeb.Workforce` — Organization & Workforce domain and use cases
- `src/backend/modules/HuGuWeb.Workforce.Infrastructure` — Workforce EF Core mapping
- `src/frontend/web` — React 19 + Vite 8 SPA
- `tests/HuGuWeb.UnitTests`
- `tests/HuGuWeb.ArchitectureTests`

## Configuration and secrets

Do not commit real passwords.

Connection string:

- Config key: `ConnectionStrings:IdentityDatabase`
- Environment override: `ConnectionStrings__IdentityDatabase`

`appsettings.Development.json` contains a clearly fake local placeholder password. Override it for your machine.

Development users (Development environment only):

```bash
dotnet user-secrets set "DevelopmentUser:Email" "dev@localhost" --project src/backend/HuGuWeb.Api
dotnet user-secrets set "DevelopmentUser:Password" "<choose-a-local-password-meeting-Identity-rules>" --project src/backend/HuGuWeb.Api
dotnet user-secrets set "DevelopmentUsers:DefaultPassword" "<same-or-other-local-password-meeting-Identity-rules>" --project src/backend/HuGuWeb.Api
```

`DevelopmentUsers:DefaultPassword` is the shared secret for additional development personas. If it is unset, those personas reuse `DevelopmentUser:Password`. Key names only — do not commit password values. Persona emails, permissions, and menus: [Development personas](DEVELOPMENT_PERSONAS.md).

Identity password rules at bootstrap: at least 12 characters, with upper, lower, digit, and non-alphanumeric characters.

If those values are missing, the API skips the affected accounts and logs the key names. There is no public registration endpoint. After permission claims change, sign out and sign in again; claims are stored in the authentication cookie.

## Database

Create the identity schema with EF Core migrations. Do not use `EnsureCreated()`. Do not apply migrations automatically in Production. Development startup also does not auto-apply migrations.

```bash
dotnet ef database update --project src/backend/HuGuWeb.Api --context AppIdentityDbContext
dotnet ef database update --project src/backend/modules/HuGuWeb.Workforce.Infrastructure --startup-project src/backend/HuGuWeb.Api --context WorkforceDbContext
```

The Identity migrations contain ASP.NET Core Identity tables and nullable `PreferredLanguage` on `AspNetUsers` (`tr` / `en` / `ru`). The Workforce migration adds Organization & Workforce tables only. Apply pending migrations with the commands above; do not auto-apply in Production.

## Backend (manual fallback)

Prefer F5 (**HuGuWeb Development**) or `.\dev.ps1`. To start the API alone:

```bash
dotnet restore
dotnet run --project src/backend/HuGuWeb.Api --launch-profile http
```

Useful URLs:

- Liveness: `http://localhost:5116/health`
- Readiness (database): `http://localhost:5116/health/ready`
- OpenAPI (Development only): `http://localhost:5116/openapi/v1.json`
- HTTPS profile is available as `https` (`https://localhost:7138`) for direct API use

## Frontend (manual fallback)

Prefer F5 (**HuGuWeb Development**) or `.\dev.ps1`. To start the SPA alone:

```bash
cd src/frontend/web
npm install
npm run dev
```

On Windows PowerShell, prefer `npm.cmd install` and `npm.cmd run dev` if `npm.ps1` is blocked by execution policy. F5 uses `npm.cmd run dev`. Do not change machine execution policy for this.

The Vite dev server proxies `/api`, `/health`, and `/openapi` to `http://localhost:5116`. Leave `VITE_API_BASE_URL` empty for that same-origin proxy. Copy `.env.example` only if you need a local override; do not commit `.env`.

## Tests

```bash
dotnet test
cd src/frontend/web
npm run lint
npm run build
```

Frontend automated tests are deferred until there is product UX behavior to protect. Current screens only prove sign-in routing.

PostgreSQL integration tests are deferred until a local or CI database is authorized. Do not treat in-memory or SQLite substitutes as identity persistence verification.

## Authentication notes

Web authentication uses an HTTP-only cookie issued by ASP.NET Core Identity. Access tokens are not stored in `localStorage`.

### Cookie strategy

| Setting | Development | Production |
| --- | --- | --- |
| Auth cookie name | `HuGuWeb.Auth` | `__Host-HuGuWeb.Auth` |
| HttpOnly | yes | yes |
| Secure | `SameAsRequest` | always |
| SameSite | Lax | Lax |
| Path | `/` | `/` |

Development uses HTTP between Vite (`http://localhost:5173`) and the API (`http://localhost:5116`) so Secure cookies are not required for local proxying. That is a local-only trade-off. Production must use HTTPS; the `__Host-` cookie prefix then requires Secure, Path `/`, and no Domain attribute. Serve the SPA and API same-site, preferably same-origin behind a reverse proxy.

Do not set `SameSite=None` to make mixed HTTP/HTTPS local origins work.

### CORS

Development origins are listed in `appsettings.Development.json`. Production `Cors:AllowedOrigins` is empty by default and denies browser origins until configured. `AllowAnyOrigin` is never combined with credentials.

### CSRF

SameSite=Lax is not enough if the SPA and API are later hosted as sibling HTTPS subdomains (same-site, cross-origin). Mutating auth endpoints therefore require ASP.NET Core antiforgery:

1. `GET /api/auth/csrf` stores the antiforgery cookie (HttpOnly) and returns the request token
2. The SPA keeps that token in memory and sends `X-XSRF-TOKEN` on POST
3. Login and logout validate the token

No custom cryptography is used.

## Observability

Logs are JSON on the console, with a correlation/request id. Incoming `X-Correlation-ID` is used only when it is a short safe token; otherwise the current trace/request id is used. OpenTelemetry ASP.NET Core tracing/metrics are registered without an external exporter.
