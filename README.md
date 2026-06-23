# Launchpad

Launchpad is a .NET 10 full-stack learning app: a fictional indie game studio
release command center for shipping Starfall Tactics v1.0.

It shows the boring modern .NET stack in one local app:

- Blazor Interactive Server and Fluent UI Blazor
- ASP.NET Core Identity with seeded roles and users
- EF Core with PostgreSQL migrations
- Aspire AppHost, ServiceDefaults, health checks, and OpenTelemetry
- Aspire Postgres MCP tools for agent-assisted database inspection
- first-party hosted workers with persisted release-check rows
- Minimal API integration endpoints protected by an API-key auth scheme
- mise-driven restore, format, build, test, and run tasks

No HTMX, no SSE, no Dagger, no external cloud services.

## First Run

Prerequisites:

- mise
- Docker Desktop with WSL integration, Docker Engine, or another Aspire-compatible container runtime

Trust mise, install pinned tools, then run:

```sh
mise trust
mise run install
mise run dev
```

Open the `launchpad-web` endpoint shown in the Aspire dashboard.

Aspire starts PostgreSQL with a persistent local data volume. To reset the demo
database, stop the AppHost and remove the Launchpad/Postgres volume from your
container runtime.

Seeded users all use:

```text
Launchpad!10
```

Useful logins:

```text
producer@launchpad.local
admin@launchpad.local
dev@launchpad.local
qa@launchpad.local
observer@launchpad.local
```

## Demo Flow

1. Log in as `producer@launchpad.local`.
2. Open the War Room.
3. Click `Start release checks`.
4. Watch Build, QA, and Security gates move through queued/running/passed.
5. Approve the release when all required gates pass, or block it with a reason.

## Commands

```sh
mise run tasks
mise run fmt
mise run fmt:check
mise run lint
mise run test
mise run check
mise run dev
```

Optional Playwright smoke tests require a running app:

```sh
LAUNCHPAD_E2E_URL=http://localhost:5195 mise run csharp:test:e2e
```

## Aspire Agent Tools

The AppHost exposes the `launchpaddb` PostgreSQL database through Aspire's
Postgres MCP server while the app is running.

```sh
dotnet tool run aspire -- mcp tools
dotnet tool run aspire -- mcp call launchpaddb-mcp execute_sql --input '{"sql":"SELECT 1 AS value;"}'
```

## Integration API

Development API key, from `appsettings.Development.json`:

```text
dev-local-key
```

Header:

```text
X-Launchpad-Api-Key: dev-local-key
```

Endpoints:

- `POST /api/integrations/check-runs/{id}/result`
- `POST /api/integrations/playtest-feedback`

OpenAPI is available at `/openapi/v1.json` in Development.
