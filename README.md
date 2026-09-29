# Personal Task Board

A lightweight, local-first personal task board (Kanban) that runs locally on Windows.
Built on ASP.NET Core (.NET 8) with **SQLite + Entity Framework Core** persistence
(see [ADR-001](docs/architecture/adrs/adr-001-sqlite-efcore.md)) and a REST API for
columns and tasks management.

## Repository structure

```
src/PersonalTaskBoard/        main app: Api/ (REST endpoints), Domain/, Data/ (EF Core),
                              Pages/, wwwroot/
src/PersonalTaskBoard.Tests/  test project: Api/, Domain/, Helpers/
docs/                         architecture (system architecture, data model, API
                              contract, ADRs), plans (implementation plan),
                              security review
```

## Run locally

```powershell
cd src/PersonalTaskBoard && dotnet run
```

## Run tests

```powershell
dotnet test src/PersonalTaskBoard.Tests/PersonalTaskBoard.Tests.csproj
```
