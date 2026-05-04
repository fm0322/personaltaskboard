# System Architecture — Personal Task Board

## 1) Scope and Goals
- Feature: **Personal Task Board**
- Request ID: `req-20260504-101447-e1c0b9`
- Runtime: Local dev on Windows 10/11, .NET 8/9
- Hosting target: `dotnet run` at `http://localhost:5000`
- Stack: ASP.NET Core Razor Pages + vanilla JavaScript + SQLite + EF Core

## 2) High-Level Architecture
```mermaid
flowchart LR
    U[User Browser] --> RP[Razor Pages UI]
    RP --> JS[Vanilla JS + SortableJS]
    JS --> API[/Minimal API Endpoints under /api/]
    RP --> API
    API --> EF[EF Core DbContext]
    EF --> DB[(SQLite: taskboard.db)]
```

## 3) Component Responsibilities
- **Razor Pages (UI composition)**  
  Server-rendered board shell, modal markup, validation summaries, task detail page.
- **Vanilla JS + SortableJS (interaction layer)**  
  Drag/drop task movement and reorder, optimistic UI updates, fetch calls to API endpoints.
- **Minimal APIs (`/api/columns`, `/api/tasks`)**  
  CRUD/reorder/move operations with validation and concurrency-safe updates.
- **EF Core (`TaskBoardDbContext`)**  
  Entity mapping, relationships, ordering persistence (`DisplayOrder`, `SortOrder`), migrations.
- **SQLite (`taskboard.db`)**  
  Local persistent store for board, columns, and tasks.

## 4) Project Structure and Folder Conventions
Recommended structure:
```text
/Pages
  /Index.cshtml(+.cs)                # Board view
  /Tasks/Details.cshtml(+.cs)        # Task detail view
/Pages/Shared                         # Shared layout/partials
/Api
  /ColumnsEndpoints.cs               # Map /api/columns*
  /TasksEndpoints.cs                 # Map /api/tasks*
/Data
  /TaskBoardDbContext.cs
  /Migrations/*                      # EF Core migrations
/Domain
  /Board.cs
  /Column.cs
  /TaskItem.cs
  /Enums/Priority.cs
/wwwroot/js
  /board.js                          # Board behaviors, API calls
  /dragdrop.js                       # SortableJS wiring for columns/cards
/wwwroot/lib/sortablejs              # Library assets (via npm/libman/CDN strategy)
```

## 5) Program.cs Startup Registration Guidance
Required startup design:
1. Register Razor Pages.
2. Register `TaskBoardDbContext` using SQLite connection string targeting `taskboard.db`.
3. Register API explorer + Swagger (dev) for contract inspection.
4. Build app pipeline: static files, routing, Razor Pages, mapped API endpoints.
5. Apply pending EF Core migrations at startup in a scoped service provider.
6. Ensure URL binding includes `http://localhost:5000` for `dotnet run`.

Configuration guidance:
- `ASPNETCORE_URLS=http://localhost:5000` or equivalent launch settings profile.
- Connection string points to local file database: `Data Source=taskboard.db`.
- In development: enable detailed errors and Swagger UI.

## 6) Static Files and JavaScript Approach
- Use **Razor Pages for HTML rendering** and **vanilla JS for client interactions**.
- Keep JS modular:
  - `board.js`: load/filter tasks, modal submit handlers, due-date visual updates.
  - `dragdrop.js`: initialize SortableJS, process drop events, call move/reorder APIs.
- Keep CSS state classes predictable:
  - `.task-overdue`, `.task-due-soon`, `.task-priority-high`, etc.
- Use unobtrusive data attributes for entity IDs and sort order.

## 7) Drag-and-Drop Integration Approach
- Library: **SortableJS** (see ADR-003).
- Design:
  - Each column task list is a Sortable container in same group.
  - On drop to different column: call `PATCH /api/tasks/{id}/move`.
  - On reorder within same column: call `PATCH /api/tasks/reorder`.
  - On column reorder: call `PATCH /api/columns/reorder`.
- Persistence behavior:
  - Client sends full ordered ID arrays and target metadata.
  - Server recomputes canonical sequential order (`SortOrder`/`DisplayOrder`) to prevent drift.

## 8) Operational Notes (Dev)
- Single local environment (`dev`) only.
- No external integration targets.
- No special compliance constraints.
- Startup behavior seeds one default board and default columns (**To Do, In Progress, Done**) if absent.

## 9) Quality Attributes
- **Maintainability:** clear separation between page rendering, API contract, and persistence.
- **Responsiveness:** optimistic drag/drop UX with server reconciliation.
- **Data integrity:** server-enforced ordering + relational constraints.
- **Simplicity:** lightweight local stack with SQLite and no external dependencies.

## 10) Functional Coverage Mapping
- **Kanban columns CRUD:** `/api/columns` + `/api/columns/{id}` with modal forms in Razor pages.
- **Task cards CRUD:** `/api/tasks` + `/api/tasks/{id}` with create/edit modals and detail page.
- **Drag/drop move and reorder:** `/api/tasks/{id}/move`, `/api/tasks/reorder`, `/api/columns/reorder`.
- **Task ordering:** persisted with `SortOrder`, recomputed server-side on reorder/move.
- **Search/filter:** `GET /api/tasks` query params `priority`, `search`, and `columnId`.
- **Due-date highlighting:** client-side CSS state from `dueDate` and current date.

