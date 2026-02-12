# Kavopici — Claude Development Guide

Start every session by reading `README.md` for full project context.

## Quick Reference

```bash
dotnet build Kavopici.sln                                    # Build
dotnet test tests/Kavopici.Tests/Kavopici.Tests.csproj       # Tests
dotnet run --project src/Kavopici.Web/Kavopici.Web.csproj    # Run (http://localhost:5201)
dotnet publish src/Kavopici.Web/Kavopici.Web.csproj -c Release -r win-x64 --self-contained  # Publish
```

## Architecture

- **Kavopici.Core** (`src/Kavopici.Core/`) — Models, DbContext, services, business logic. No web dependencies.
- **Kavopici.Web** (`src/Kavopici.Web/`) — Blazor Server app. Components (Pages, Shared, Layout), AppState, UpdateService.
- **Kavopici.Tests** (`tests/Kavopici.Tests/`) — xUnit unit tests for Core services.

## Development Rules

### Before committing
- Run `dotnet build Kavopici.sln` — must pass with zero errors.
- Run `dotnet test` — all tests must pass.
- Check if `README.md` needs updating (new features, changed architecture, new setup steps). Update it if so.

### Code conventions
- **UI strings**: Czech. All user-facing text in Razor components is in Czech.
- **Code, comments, variable names**: English.
- **Commit messages**: Imperative mood, action verb prefix — `Add`, `Fix`, `Update`, `Remove`, `Rename`.

### Database safety — CRITICAL
- **Never introduce breaking changes** to the production database (dropping columns, renaming tables, deleting seed data).
- **Never edit existing migration files.** Always create a new migration.
- **Destructive schema changes** (drop column, drop table, change column type) require explicit user approval.
- **Adding** new tables, columns, indexes, or seed data is safe.
- Migration workflow:
  ```bash
  cd src/Kavopici.Core
  dotnet ef migrations add YourMigrationName
  ```
- The app auto-applies migrations on startup (`db.Database.Migrate()`).

## Testing Conventions

- Framework: **xUnit** with in-memory SQLite (`Data Source=:memory:`).
- Tests live in `tests/Kavopici.Tests/Services/` — one file per service.
- Use `TestDbContextFactory` from `Helpers/` to create isolated test databases.
- Each test class implements `IDisposable` for cleanup.
- Follow existing patterns: `[Fact]` methods, `SetupAsync()` helpers, `Assert.ThrowsAsync<>()` for error cases.
- When adding a new service, add a corresponding test file.
