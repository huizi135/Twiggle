# Local Development — W2

This is a local-only development application, not a deployed product. Manual sessions, original answers, reviewed facts, and episodes are now persisted. Hosted authentication and uploads are not implemented. For opt-in AI processing see [AI Interview Setup](ai-interview.md). See [First Saved Experience](first-experience.md).

## Prerequisites

- Git for Windows and access to this private repository.
- Node.js 24 LTS and npm.
- .NET 10 SDK.
- Docker Desktop with Linux containers and Docker Compose available.

Official references checked during setup: [Vite prerequisites](https://vite.dev/guide/), [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy), [Npgsql EF Core 10](https://www.npgsql.org/efcore/release-notes/10.0.html).

## 1. Update and configure

In PowerShell:

```powershell
Set-Location "C:\Users\Admin\Documents\GitHub\Twiggle"
git status
git pull --ff-only
Copy-Item .env.example .env
notepad .env
```

Only copy the example if `.env` does not exist. Set a local database password without quotes or semicolons. Never commit `.env`. The password is only for your local development database; do not reuse an account password.

## 2. Start PostgreSQL

From the repository root:

```powershell
docker compose up -d --wait
docker compose ps
```

Database endpoint: `127.0.0.1:54329`, database/user: `twiggle`. The volume preserves local data across container restarts. Changing the password in `.env` does not change an existing database role password; keep the original value or explicitly manage the role. Avoid `docker compose down -v` unless you intend to erase local database data.

The API startup script applies the initial EF Core migration before serving requests. Keep PostgreSQL running first. No EF CLI installation is required.

## 3. Start the API

In a second PowerShell terminal, from the repository root:

```powershell
.\scripts\start-api.ps1
```

If local script execution is blocked, use this process-only invocation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-api.ps1
```

The script reads `.env`, configures the connection, and runs ASP.NET Core at `http://127.0.0.1:5080`. Credentials are not printed. The API intentionally refuses to start outside `Development`; it binds only to loopback and contains no hosted authentication. Do not expose it through a tunnel or change the binding for public use.

## 4. Start the website

In a third PowerShell terminal:

```powershell
Set-Location "C:\Users\Admin\Documents\GitHub\Twiggle\apps\web"
npm ci
npm run dev
```

Open **http://127.0.0.1:5173/app**. Vite proxies `/api` requests to the local backend. All five workspace routes are available. Discover and Experience Bank support manual capture and review. Capabilities and Settings remain explicitly labeled placeholders.

The production frontend build is a bundle, not a configured deployment. `npm run preview` has no backend API proxy. Hosted routing fallback, authentication, HTTPS, and same-origin API serving must be configured before deployment.

## 5. Verify

```powershell
Invoke-RestMethod http://127.0.0.1:5080/api/health/live
Invoke-RestMethod http://127.0.0.1:5080/api/health/ready
Invoke-RestMethod http://127.0.0.1:5080/api/workspace
```

Expected: live reports `healthy`; ready reports `ready` and `database: connected`; workspace reports `development-only`, enabled manual interviews/persistence, and AI availability matching your server configuration. Home should show two connected service statuses.

Stop the database with `docker compose stop postgres`. Readiness must return HTTP 503 while liveness remains HTTP 200. Restart with `docker compose start postgres`, then use **Check connection again** on Home. This separates an API-process problem from a database dependency problem.

Frontend checks:

```powershell
Set-Location apps\web
npm run lint
npm run build
```

Backend build, from repository root:

```powershell
dotnet build src/Twiggle.Api --configuration Release
```

## Troubleshooting

- Website loads but API unavailable: check API terminal and port 5080 before changing database settings.
- API healthy but database unavailable: check Docker Desktop, container health, port 54329, and matching credentials.
- Port in use: stop the conflicting local process or coordinate both proxy and API changes; do not randomly change one endpoint.
- `dotnet` not recognized: install .NET 10 **SDK**, then reopen the terminal.
- PowerShell cannot run `npm.ps1`: use `npm.cmd ci` and `npm.cmd run dev`.

## Verification record

Frontend production build and lint passed; API Release build passed with no warnings/errors. Live HTTP and browser checks are recorded in `docs/w2-verification.md`. This environment has no Docker, so successful PostgreSQL container startup and connectivity must be verified on a machine with Docker. A CI workflow checks web lint/build and API compilation; its execution result must be inspected on GitHub, not assumed from the workflow file.

## Next slice

Add adaptive questioning and candidate extraction behind the existing review boundary. Do not promote model interpretations directly into accepted facts or skills.
