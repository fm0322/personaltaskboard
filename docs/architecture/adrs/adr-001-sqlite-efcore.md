# ADR-001: Use SQLite with EF Core for persistence

## Title
Use SQLite (`taskboard.db`) with Entity Framework Core for local persistence.

## Status
Accepted

## Context
The Personal Task Board is a local-first dev-scoped application with no external integration targets and no enterprise-scale operational constraints. It needs durable persistence for board/column/task data, simple setup, schema evolution, and developer-friendly querying. Startup should remain frictionless with `dotnet run`.

## Decision
Adopt:
- **SQLite** as the relational data store in file `taskboard.db`
- **EF Core** as ORM and migration mechanism
- Automatic migration application on startup (`Database.Migrate()`)

## Consequences
Positive:
- Zero infrastructure dependency; ideal for local Windows usage.
- Strong relational integrity for board/column/task relationships.
- EF Core migrations provide controlled schema evolution.
- Easy backup/move via single DB file.

Negative:
- Limited concurrent write scalability compared with server DB engines.
- SQLite-specific SQL behavior can differ from SQL Server/PostgreSQL.
- Search performance/features are basic unless FTS is explicitly added.

## Alternatives Considered
1. **In-memory only**
   - Rejected: no durable storage.
2. **JSON file persistence**
   - Rejected: weak concurrency handling and relational integrity.
3. **SQL Server LocalDB**
   - Rejected: heavier setup and local dependency overhead vs SQLite.
4. **LiteDB / document DB**
   - Rejected: does not naturally align with relational ordering constraints and FK semantics needed for columns/tasks.

