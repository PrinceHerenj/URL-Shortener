# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A URL shortener built with **ASP.NET Core Minimal APIs** (no controllers), **SQLite** via **Entity Framework Core**, targeting **.NET 8**. There is no frontend — this is a pure JSON API.

Three endpoints, all defined in `src/Program.cs`:
- `POST /shorten` — accept `{ "url": "..." }`, return a 6-char code + short URL (`201 Created`).
- `GET /r/{code}` — look up the code, increment `ClickCount`, `302` redirect to the original URL.
- `GET /analytics/{code}` — return click count + metadata (read-only).

A detailed walkthrough of every concept and file lives in `src/PROJECT-GUIDE.md`.

## Layout

```
SmartUrlShortener.sln              # solution at repo root
src/                               # the web project (SmartUrlShortener.csproj)
  Program.cs                       # entire app: DI setup + all endpoints (top-level statements)
  Data/AppDbContext.cs             # EF Core DbContext
  Models/UrlMapping.cs             # DB entity
  Models/ShortenRequest.cs         # request DTO (positional record)
  appsettings.json                 # base config
  appsettings.Development.json     # adds ConnectionStrings:DefaultConnection
  urlshortener.db                  # SQLite file, created at runtime
tests/SmartUrlShortener.Tests/     # xUnit tests
```

## Commands

Run from the relevant directory, or use `--project`. The web project lives in `src/`, so a bare `dotnet run` from the repo root will fail — cd into `src/` or pass the path.

```bash
# Run the API (Development profile → http://localhost:5050)
dotnet run --project src/SmartUrlShortener.csproj

# Build
dotnet build SmartUrlShortener.sln

# Run all tests
dotnet test

# Run a single test (filter by fully-qualified name)
dotnet test --filter "FullyQualifiedName~UrlShortenerApiTests"
```

Exercise the API locally:

```bash
curl -X POST http://localhost:5050/shorten -H "Content-Type: application/json" -d '{"url":"https://example.com"}'
curl -i http://localhost:5050/r/<code>          # 302 redirect
curl http://localhost:5050/analytics/<code>
```

Deploy to Azure (defaults are cached in `src/.azure/config`):

```bash
az webapp up              # first run needs --name/--location/--sku; re-deploy reuses defaults
```

## Key architecture notes

- **All application logic is in `src/Program.cs`.** Top-level statements build the host, register `AppDbContext`, call `EnsureCreated()`, then map three minimal-API endpoints. There is no `Migrations/` folder — the schema comes from `db.Database.EnsureCreated()`, which only creates the DB if it doesn't exist. **Changing the model does not alter an existing `urlshortener.db`**; to pick up schema changes locally, delete the `.db` file and restart.

- **Connection string**: `Program.cs` reads `ConnectionStrings:DefaultConnection`, falling back to `Data Source=urlshortener.db`. Only `appsettings.Development.json` sets it, so non-Development environments always fall back to the local file. The `??` fallback is the null-coalescing operator.

- **Short-code generation**: first 6 chars of a GUID (`Guid.NewGuid().ToString("N")[..6]`), not a counter. `AppDbContext.OnModelCreating` enforces a **unique index on `ShortCode`** as the collision safety net — `Program.cs` itself does not check for collisions before inserting.

- **The `public partial class Program { }`** at the bottom of `Program.cs` exists solely so integration tests can reference `Program` via `WebApplicationFactory<Program>` (top-level statements otherwise produce no accessible `Program` type). Don't remove it.

- **`ShortenRequest` is a positional record** (`record ShortenRequest(string Url)`), bound case-insensitively from the JSON `url` field.

- **Tests** use xUnit with `WebApplicationFactory<Program>`. The API test fixture swaps the production SQLite registration for an isolated in-memory database (`UseInMemoryDatabase`) so each test gets a fresh store. Note: `ValidationTests` is a pure unit test that does not hit the web host; its validation logic is duplicated inline rather than calling the endpoint.
