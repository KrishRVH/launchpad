# Agent Guide

This repo follows the local `standards/` C# and mise conventions, with Dagger
intentionally omitted.

## Commands

- `mise run tasks`: list tasks.
- `mise run install`: install pinned tools and restore dependencies.
- `mise run dev`: run the Aspire AppHost.
- `mise run fmt`: format.
- `mise run fmt:check`: verify formatting.
- `mise run lint`: Release build with analyzers.
- `mise run test`: unit/integration-safe tests.
- `mise run check`: normal local gate.

Use mise for developer commands. If mise asks for trust, run `mise trust` from
the repo root.

## Shape

- `src/Launchpad.AppHost`: Aspire orchestration.
- `src/Launchpad.ServiceDefaults`: health checks, service discovery, telemetry.
- `src/Launchpad.Web`: Blazor, Identity, EF Core, workers, Minimal APIs.
- `tests/Launchpad.Tests`: normal test gate.
- `tests/Launchpad.PlaywrightTests`: optional smoke tests for a running app.

Keep the app idiomatic .NET, not framework-collector .NET. Prefer Blazor,
ASP.NET Core, EF Core, Identity, hosted services, and Aspire before adding third
party infrastructure.

## Standards Deviations

This app intentionally keeps Blazor's implicit usings enabled and relaxes a few
template-hostile analyzer rules in `.editorconfig`. The local standards repo is
stricter for library code, but the generated Blazor/Identity surface is clearer
and closer to current .NET templates with these targeted exceptions.

Demo migration/seeding only runs in Development, or when
`Launchpad:SeedDemoData=true` is set explicitly.
