# Implementation Plan — Personal Task Board
**Request ID**: `req-20260504-101447-e1c0b9`
**Date**: 2026-05-04
**Architect Artifacts**: `docs/architecture/manifest-req-20260504-101447-e1c0b9.yaml`
**Architect Branch**: `docs/architecture-req-20260504-101447-e1c0b9`
**Architect Commit**: `8d61635ddd0a24e6ea46189301d20e3b18412024`

---

## Summary

Build a lightweight personal Kanban task board running locally via `dotnet run` on http://localhost:5000.  
Stack: ASP.NET Core 8 Razor Pages + Minimal APIs + EF Core + SQLite + SortableJS (vanilla JS).

**Total estimated effort**: ~44 hours (agent execution time across all tasks)  
**Critical path**: ~42 hours (fully sequential project scope through Phase 4; QA + Security parallel in Phase 5)  
**Parallelization factor**: ~1.05× (QA + Security write to different scopes, can run concurrently)

> **Rubber Duck Critique Applied (2026-05-04)**: 13 findings incorporated — dependency graph fixed, read_only flags corrected, file lists completed, EF tooling prerequisite added, SortableJS version pinned, Swagger added, effort totals recalculated, acceptance criteria sharpened with negative-path cases.

---

## Requirements Coverage

| Category | Count |
|---|---|
| Must Have features | 7 |
| Should Have features | 3 |
| Out of Scope | 4 |
| Must Have mapped to tasks | 7/7 |
| Should Have mapped to tasks | 3/3 |

---

## Architecture References

| Artifact | Path |
|---|---|
| System Architecture | `docs/architecture/system-architecture-req-20260504-101447-e1c0b9.md` |
| Data Model | `docs/architecture/data-model-req-20260504-101447-e1c0b9.md` |
| API Contract | `docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml` |
| ADR-001 SQLite+EF Core | `docs/architecture/adrs/adr-001-sqlite-efcore.md` |
| ADR-002 Razor Pages | `docs/architecture/adrs/adr-002-razor-pages-vs-spa.md` |
| ADR-003 SortableJS | `docs/architecture/adrs/adr-003-dragdrop-library.md` |

---

## Phase 0: Architecture (Complete ✓)

Architect artifacts created and committed.  
**No implementation tasks required** — reference artifacts above.

---

## Phase 1: Project Foundation (Sequential)

All steps share `project_scope: PersonalTaskBoard` — must run sequentially.

### step-001 — Scaffold .NET 8 Project

**Assignee**: Coder  
**Type**: backend  
**Estimated hours**: 2  
**Depends on**: _(none)_  
**Requirements addressed**: All (foundational)

**Files**:
- `PersonalTaskBoard/PersonalTaskBoard.csproj`
- `PersonalTaskBoard/PersonalTaskBoard.sln`
- `PersonalTaskBoard/Program.cs`
- `PersonalTaskBoard/Properties/launchSettings.json`
- `PersonalTaskBoard/appsettings.json`
- `PersonalTaskBoard/appsettings.Development.json`
- `.gitignore`

**Acceptance Criteria**:
- [ ] `dotnet new webapp` or equivalent scaffold creates `PersonalTaskBoard.csproj` targeting `net8.0`
- [ ] `launchSettings.json` sets `applicationUrl: "http://localhost:5000"` for the default profile
- [ ] `appsettings.json` contains `ConnectionStrings.DefaultConnection: "Data Source=taskboard.db"`
- [ ] `.gitignore` excludes `bin/`, `obj/`, `*.db`, `*.db-shm`, `*.db-wal`
- [ ] `dotnet run` starts without errors (serves default Razor Pages 404 page)
- [ ] Solution file references the project

---

### step-002 — Domain Entities

**Assignee**: Coder  
**Type**: backend  
**Estimated hours**: 2  
**Depends on**: `step-001`  
**Requirements addressed**: FR-1 (columns), FR-2 (task cards), FR-8 (task ordering)  
**Architect spec**: `docs/architecture/data-model-req-20260504-101447-e1c0b9.md`

**Files**:
- `PersonalTaskBoard/Domain/Board.cs`
- `PersonalTaskBoard/Domain/Column.cs`
- `PersonalTaskBoard/Domain/TaskItem.cs`
- `PersonalTaskBoard/Domain/Enums/Priority.cs`

**Acceptance Criteria**:
- [ ] `Board`: `Id (Guid PK)`, `Name (string, max 100)`, `CreatedAt`, `UpdatedAt (DateTimeOffset)`
- [ ] `Column`: `Id`, `BoardId (FK)`, `Name (max 80)`, `DisplayOrder (int)`, `WipLimit (int?)`, `CreatedAt`, `UpdatedAt`
- [ ] `TaskItem`: `Id`, `ColumnId (FK)`, `Title (max 200)`, `Description (max 4000, nullable)`, `DueDate (DateOnly?)`, `Priority (Priority enum)`, `AssigneeLabel (max 60, nullable)`, `SortOrder (int)`, `CreatedAt`, `UpdatedAt`
- [ ] `Priority` enum: `Low=0, Medium=1, High=2, Urgent=3`
- [ ] Navigation properties present: `Board.Columns`, `Column.Tasks`, `Column.Board`, `TaskItem.Column`
- [ ] All entities are in `PersonalTaskBoard.Domain` namespace

---

### step-003 — DbContext & EF Core Configuration

**Assignee**: Coder  
**Type**: backend  
**Estimated hours**: 3  
**Depends on**: `step-002`  
**Requirements addressed**: FR-4 (SQLite persistence)  
**Architect spec**: `docs/architecture/data-model-req-20260504-101447-e1c0b9.md` §4

**Files**:
- `PersonalTaskBoard/Data/TaskBoardDbContext.cs`
- `PersonalTaskBoard/PersonalTaskBoard.csproj` _(add NuGet refs)_
- `PersonalTaskBoard/Program.cs` _(register DbContext + Swagger)_

**NuGet packages to add**:
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools` _(required for `dotnet ef migrations add`)_
- `Swashbuckle.AspNetCore` _(Swagger UI in development)_

**EF Core tooling prerequisite**:
- Global tool must be available: `dotnet tool install --global dotnet-ef`
- Coder agent must verify `dotnet ef --version` succeeds before running migrations in step-004

**Acceptance Criteria**:
- [ ] `TaskBoardDbContext` exposes `DbSet<Board>`, `DbSet<Column>`, `DbSet<TaskItem>`
- [ ] `OnModelCreating` configures:
  - Required fields and max lengths per data model spec
  - `Priority` stored as `int`
  - Cascade delete: Board→Column, Column→TaskItem
  - Unique index: `Column(BoardId, DisplayOrder)`
  - Unique index: `TaskItem(ColumnId, SortOrder)`
  - Unique index: `Column(BoardId, Name)`
  - Index on `TaskItem.Priority`
  - Index on `TaskItem.Title`
- [ ] `Program.cs` registers DbContext: `builder.Services.AddDbContext<TaskBoardDbContext>(opt => opt.UseSqlite(connectionString))`
- [ ] `Program.cs` registers Swagger: `builder.Services.AddEndpointsApiExplorer()` + `builder.Services.AddSwaggerGen()` and maps `app.UseSwagger()` / `app.UseSwaggerUI()` inside `if (app.Environment.IsDevelopment())`
- [ ] `dotnet ef --version` confirms tooling is available
- [ ] `dotnet build` succeeds with no warnings

---

### step-004 — EF Core Migration & Startup Seed

**Assignee**: Coder  
**Type**: backend  
**Estimated hours**: 3  
**Depends on**: `step-003`  
**Requirements addressed**: FR-4 (persistence), FR-1 (default columns)  
**Architect spec**: `docs/architecture/data-model-req-20260504-101447-e1c0b9.md` §5

**Files**:
- `PersonalTaskBoard/Data/Migrations/` _(generated migration files)_
- `PersonalTaskBoard/Program.cs` _(updated: migrate + seed on startup)_

**Acceptance Criteria**:
- [ ] Initial migration created: `dotnet ef migrations add InitialCreate` produces valid migration files
- [ ] `Program.cs` calls `Database.Migrate()` in a scoped service provider before `app.Run()`
- [ ] Seed logic inserts one default `Board` named "My Board" if no boards exist
- [ ] Seed creates three default columns: "To Do" (DisplayOrder=0), "In Progress" (DisplayOrder=1), "Done" (DisplayOrder=2) linked to the default board
- [ ] `dotnet run` starts cleanly, creates `taskboard.db`, and seeds defaults on first run
- [ ] Re-running `dotnet run` does NOT create duplicate default data (idempotent seed)

---

## Phase 2: API Endpoints (Sequential)

### step-005 — Columns Minimal API Endpoints

**Assignee**: Coder  
**Type**: backend  
**Estimated hours**: 4  
**Depends on**: `step-004`  
**Requirements addressed**: FR-1 (column CRUD), FR-6 (column CRUD)  
**Architect spec**: `docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml` `/api/columns`

**Files**:
- `PersonalTaskBoard/Api/ColumnsEndpoints.cs`
- `PersonalTaskBoard/Api/DTOs/ColumnDtos.cs`
- `PersonalTaskBoard/Program.cs` _(register endpoint group)_

**Acceptance Criteria**:
- [ ] `GET /api/columns` returns all columns for the default board, ordered by `DisplayOrder`, HTTP 200
- [ ] `POST /api/columns` creates a column; assigns next `DisplayOrder`; returns HTTP 201 with column object
- [ ] `PUT /api/columns/{id}` updates `Name` and/or `WipLimit`; returns HTTP 200; returns HTTP 404 if not found
- [ ] `DELETE /api/columns/{id}` deletes column and cascades to tasks; returns HTTP 204; HTTP 404 if not found
- [ ] `PATCH /api/columns/reorder` accepts `{ ids: [guid, ...] }` and recomputes `DisplayOrder` sequentially (0,1,2…); returns HTTP 204
- [ ] Request validation: `Name` required and ≤ 80 chars; returns HTTP 400 with error message on failure
- [ ] **Negative path**: `POST /api/columns` with missing `Name` returns HTTP 400
- [ ] **Negative path**: `PUT /api/columns/{id}` with `Name` exceeding 80 chars returns HTTP 400
- [ ] **Negative path**: `WipLimit` of 0 or negative returns HTTP 400 (must be ≥ 1 when set)
- [ ] **Negative path**: `PUT /api/columns/{unknownId}` returns HTTP 404
- [ ] DTO classes: `ColumnDto`, `CreateColumnRequest`, `UpdateColumnRequest`, `ReorderRequest`
- [ ] All DTOs and status codes match `docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml`

---

### step-006 — Tasks Minimal API Endpoints

**Assignee**: Coder  
**Type**: backend  
**Estimated hours**: 5  
**Depends on**: `step-005`  
**Requirements addressed**: FR-2 (task cards), FR-5 (task CRUD), FR-3 (move), FR-8 (ordering), FR-9 (filter), FR-7 (detail)  
**Architect spec**: `docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml` `/api/tasks`

**Files**:
- `PersonalTaskBoard/Api/TasksEndpoints.cs`
- `PersonalTaskBoard/Api/DTOs/TaskDtos.cs`
- `PersonalTaskBoard/Program.cs` _(register tasks endpoint group)_

**Acceptance Criteria**:
- [ ] `GET /api/tasks` returns tasks; supports query params: `columnId`, `priority` (string: Low/Medium/High/Urgent per API contract), `search` (title contains)
- [ ] `POST /api/tasks` creates task in specified column at bottom of column (next SortOrder); returns HTTP 201
- [ ] `GET /api/tasks/{id}` returns full task detail; HTTP 404 if not found
- [ ] `PUT /api/tasks/{id}` updates all editable fields (Title, Description, DueDate, Priority, AssigneeLabel); returns HTTP 200; HTTP 404 if not found
- [ ] `DELETE /api/tasks/{id}` deletes task; returns HTTP 204; HTTP 404 if not found
- [ ] `PATCH /api/tasks/{id}/move` accepts `{ targetColumnId, targetIndex }` (per API contract naming) — moves task to new column at the given index, recomputing SortOrder for affected tasks; returns HTTP 200
- [ ] `PATCH /api/tasks/reorder` accepts `{ columnId, orderedIds: [guid, ...] }` and recomputes `SortOrder`; returns HTTP 204
- [ ] Request validation: `Title` required and ≤ 200 chars; `Priority` must be a valid string value (Low/Medium/High/Urgent); returns HTTP 400 on failure
- [ ] **Negative path**: `POST /api/tasks` with missing `Title` returns HTTP 400
- [ ] **Negative path**: `POST /api/tasks` with invalid `priority` value returns HTTP 400
- [ ] **Negative path**: `PATCH /api/tasks/{id}/move` with non-existent `targetColumnId` returns HTTP 404
- [ ] **Negative path**: `PUT /api/tasks/{unknownId}` returns HTTP 404
- [ ] DTO classes: `TaskDto`, `TaskSummaryDto`, `CreateTaskRequest`, `UpdateTaskRequest`, `MoveTaskRequest`, `ReorderTasksRequest`
- [ ] All DTOs and status codes match `docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml`

---

## Phase 3: Razor Pages — Board View (Sequential)

### step-007 — Shared Layout & Board Index Page

**Assignee**: Coder + Designer  
**Type**: frontend  
**Estimated hours**: 5  
**Depends on**: `step-006`  
**Requirements addressed**: FR-1 (board view / columns), FR-2 (task cards), FR-5 (create/delete modals), FR-10 (due date highlight)

**Files**:
- `PersonalTaskBoard/Pages/Shared/_Layout.cshtml`
- `PersonalTaskBoard/Pages/Shared/_ViewImports.cshtml`
- `PersonalTaskBoard/Pages/Shared/_ViewStart.cshtml`
- `PersonalTaskBoard/Pages/Index.cshtml`
- `PersonalTaskBoard/Pages/Index.cshtml.cs`
- `PersonalTaskBoard/wwwroot/css/site.css`

**Acceptance Criteria**:
- [ ] Layout includes `<link>` for `site.css` and `<script src>` for `board.js` and `dragdrop.js`
- [ ] Layout loads SortableJS from CDN at a **pinned version** (e.g. `https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js`) with matching SRI integrity hash — `@latest` is prohibited
- [ ] Index page renders column container elements with `data-column-id` attributes; task card DOM is populated by `board.js` on page load (not server-rendered individually)
- [ ] Column container scaffold (headers, task list `<ul>`, "Add Task" button) is server-rendered by `Index.cshtml`
- [ ] Each task card (injected by JS) shows: title, priority badge (colour-coded), due date (if set), assignee label (if set)
- [ ] Overdue tasks (`DueDate < today`) have `.task-overdue` CSS class applied (red border/background)
- [ ] "Add Column" button triggers a modal; "Add Task" button per column triggers a create-task modal
- [ ] Delete column button present per column header (with confirmation)
- [ ] Filter bar at top: search input + priority dropdown; triggers client-side filtering
- [ ] CSS classes defined: `.task-overdue`, `.task-due-soon` (within 3 days), `.priority-low`, `.priority-medium`, `.priority-high`, `.priority-urgent`
- [ ] Board is usable at ≥1024px viewport width (columns visible without horizontal scrolling for ≤5 columns)

---

### step-008 — Task Detail Page

**Assignee**: Coder + Designer  
**Type**: frontend  
**Estimated hours**: 3  
**Depends on**: `step-007`  
**Requirements addressed**: FR-7 (task detail view), FR-5 (edit task)

**Files**:
- `PersonalTaskBoard/Pages/Tasks/Details.cshtml`
- `PersonalTaskBoard/Pages/Tasks/Details.cshtml.cs`

**Acceptance Criteria**:
- [ ] Route: `/tasks/details?id={taskId}`
- [ ] Displays all task fields: Title, Description, Priority, DueDate, AssigneeLabel, Column (read), CreatedAt, UpdatedAt
- [ ] Edit form allows modifying: Title, Description, DueDate, Priority, AssigneeLabel
- [ ] Save button calls `PUT /api/tasks/{id}` via JS or Razor Page handler; shows success/error feedback
- [ ] Delete button calls `DELETE /api/tasks/{id}` then redirects to `/`
- [ ] "Back to Board" link returns to `/`
- [ ] Overdue indicator shown if applicable

---

## Phase 4: Frontend JavaScript (Sequential)

### step-009 — board.js — Board Interactivity

**Assignee**: Coder  
**Type**: frontend  
**Estimated hours**: 5  
**Depends on**: `step-007`, `step-006` _(explicit: board.js calls API endpoints from step-006)_  
**Requirements addressed**: FR-5 (CRUD modals), FR-6 (column CRUD), FR-9 (search/filter), FR-10 (due date highlights)

**Files**:
- `PersonalTaskBoard/wwwroot/js/board.js`

**Acceptance Criteria**:
- [ ] On page load, fetches `GET /api/tasks` and renders task cards into column containers
- [ ] "Add Task" modal: submits `POST /api/tasks`; on success, appends new card to column without page reload
- [ ] "Edit Column" modal: submits `PUT /api/columns/{id}`; updates column header text in place
- [ ] "Add Column" modal: submits `POST /api/columns`; appends new column lane to board
- [ ] "Delete Column" confirmation: calls `DELETE /api/columns/{id}`; removes column DOM element
- [ ] Search input filters visible cards by title (client-side, case-insensitive) in real time
- [ ] Priority dropdown filters visible cards by priority level; "All" option shows all
- [ ] Due-date CSS classes (`.task-overdue`, `.task-due-soon`) computed on render using `Date.now()`
- [ ] Click on task card navigates to `/tasks/details?id={taskId}`
- [ ] Error states: failed API calls show a brief dismissible error banner

---

### step-010 — dragdrop.js — SortableJS Integration

**Assignee**: Coder  
**Type**: frontend  
**Estimated hours**: 4  
**Depends on**: `step-009`  
**Requirements addressed**: FR-3 (drag and drop), FR-8 (card ordering)  
**Architect spec**: `docs/architecture/adrs/adr-003-dragdrop-library.md`

**Files**:
- `PersonalTaskBoard/wwwroot/js/dragdrop.js`
- `PersonalTaskBoard/wwwroot/css/site.css` _(add drag ghost/placeholder CSS rules)_

**Acceptance Criteria**:
- [ ] All column task lists initialised as `Sortable` instances in the same group (e.g. `group: "tasks"`)
- [ ] Column headers initialised as a separate sortable container for column reordering
- [ ] On card drop to **same column**: calls `PATCH /api/tasks/reorder` with `{ columnId, orderedIds }` using current DOM order
- [ ] On card drop to **different column**: calls `PATCH /api/tasks/{id}/move` with `{ targetColumnId, targetIndex }` (matching API contract); UI updates optimistically
- [ ] On column reorder: calls `PATCH /api/columns/reorder` with `{ ids }` in new DOM order
- [ ] If API call fails, DOM reverts to previous state (cancel move)
- [ ] Drag ghost/placeholder styled with `.sortable-ghost` CSS class (reduced opacity; dashed border outline defined in `site.css`)
- [ ] Drag cursor styled with `.sortable-drag` CSS class
- [ ] Touch support enabled (`SortableJS` touch config)

---

## Phase 5: QA & Security Review (Parallel)

`step-011` writes to `project_scope: PersonalTaskBoard.Tests` and `step-012` writes to `docs/security/` — different scopes with no file overlap → **PARALLEL ALLOWED** even though both are `read_only: false`.

### step-011 — QA Validation

**Assignee**: QA  
**Type**: qa  
**Estimated hours**: 4  
**Depends on**: `step-010`, `step-008` _(full feature coverage requires task detail page complete)_  
**Read only**: false _(creates test files)_  
**Requirements addressed**: All must-have + should-have features

**Files** _(test files created)_:
- `PersonalTaskBoard.Tests/PersonalTaskBoard.Tests.csproj`
- `PersonalTaskBoard.Tests/Api/ColumnsEndpointsTests.cs`
- `PersonalTaskBoard.Tests/Api/TasksEndpointsTests.cs`
- `PersonalTaskBoard.Tests/Domain/TaskItemTests.cs`
- `PersonalTaskBoard.sln` _(add test project reference)_

**Acceptance Criteria**:
- [ ] Test project uses xUnit + `Microsoft.EntityFrameworkCore.Sqlite` with in-memory SQLite connection string (`Data Source=:memory:`) for integration-style tests (validates relational constraints, not just in-memory ORM behavior)
- [ ] Unit tests for `ColumnsEndpoints`: create, update, delete, reorder — all happy paths + 404 cases
- [ ] Unit tests for `ColumnsEndpoints`: negative paths — missing Name (400), WipLimit=0 (400), unknown ID on update (404)
- [ ] Unit tests for `TasksEndpoints`: CRUD + move + reorder + filter query params (columnId, priority string, search)
- [ ] Unit tests for `TasksEndpoints`: negative paths — missing Title (400), invalid priority string (400), unknown targetColumnId on move (404)
- [ ] Unit test for seed logic: running seed twice yields same row count (idempotent)
- [ ] Unit test for `Priority` enum boundary: values 0–3 valid, value 4 invalid
- [ ] All tests pass: `dotnet test`
- [ ] Test project and solution wiring added (`PersonalTaskBoard.sln` references test project)

---

### step-012 — Security Review

**Assignee**: SecurityReviewAgent  
**Type**: security  
**Estimated hours**: 2  
**Depends on**: `step-010`, `step-008` _(all implementation must be complete before review)_  
**Read only**: false _(writes security findings document)_  
**Requirements addressed**: Data integrity, local security posture

**Files**:
- `PersonalTaskBoard/` _(read only — source files reviewed but not modified)_
- `docs/security/security-review-req-20260504-101447-e1c0b9.md` _(written by this step)_

**Acceptance Criteria**:
- [ ] Confirm no sensitive data (secrets, API keys) committed to source
- [ ] Confirm SQLite file path is relative (local only, no network exposure)
- [ ] Confirm no SQL injection vectors (EF Core parameterised queries used throughout)
- [ ] Confirm input validation on all API endpoints (max lengths, required fields)
- [ ] Confirm no dangerous file operations from user input
- [ ] Security findings documented in `docs/security/security-review-req-20260504-101447-e1c0b9.md`
- [ ] Any critical findings must be resolved before Phase 6

---

## Phase 6: Code Review & Pull Request (Sequential)

### step-013 — Code Review & PR to dev

**Assignee**: CodeReviewAgent  
**Type**: docs  
**Estimated hours**: 2  
**Depends on**: `step-011`, `step-012`  
**Requires git workspace**: true

**Files**: _(all implementation files — read for review)_

**Acceptance Criteria**:
- [ ] All implementation files reviewed for correctness, readability, and adherence to architecture spec
- [ ] PR created targeting `dev` branch
- [ ] PR title: `feat: Personal task board — initial implementation`
- [ ] PR description includes: feature summary, architecture doc references, how to run (`dotnet run`), how to test
- [ ] No blocking review comments before merge
- [ ] Branch: `feat/personal-task-board-req-20260504-101447-e1c0b9`

---

## Parallelization Summary

| Phase | Tasks | Parallel? | Reason |
|---|---|---|---|
| Phase 0 | Architecture (done) | N/A | Complete |
| Phase 1 | step-001 → 002 → 003 → 004 | Sequential | Dependency chain |
| Phase 2 | step-005 → 006 | Sequential | Same project scope + dependency |
| Phase 3 | step-007 → 008 | Sequential | Same project scope + dependency |
| Phase 4 | step-009 → 010 | Sequential | Same project scope + dependency |
| Phase 5 | step-011 ∥ step-012 | **Parallel** | Different project scopes (`PersonalTaskBoard.Tests` vs `docs/security/`) — no file overlap |
| Phase 6 | step-013 | Sequential | Git workspace exclusive |

---

## Conflict Detection Diagnostics

- **step-001 to step-010**: All in `project_scope: PersonalTaskBoard` with `read_only: false` → **SEQUENTIAL** (project scope conflict rule)
- **step-009 explicitly depends on step-006**: Ensures API endpoints exist before board.js is written
- **step-011 and step-012 both depend on step-008 and step-010**: Ensures full feature set is implemented before QA and security review begin
- **step-011 vs step-012**: Different project scopes (`PersonalTaskBoard.Tests` vs `docs/security/`), no file overlap → **PARALLEL ALLOWED** despite both being `read_only: false`
- **step-013**: `requires_git_workspace: true` → **SEQUENTIAL**, runs alone (git workspace exclusivity)

---

## Task JSON

```json
{
  "task_id": "req-20260504-101447-e1c0b9",
  "implementation_plan_doc": "docs/plans/implementation-plan-req-20260504-101447-e1c0b9.md",
  "manifest": "docs/plans/manifest-req-20260504-101447-e1c0b9.yaml",
  "tasks": [
    {
      "id": "step-001",
      "title": "Scaffold .NET 8 Project",
      "description": "Create PersonalTaskBoard ASP.NET Core project, solution file, launchSettings.json with port 5000, appsettings.json with SQLite connection string, .gitignore",
      "type": "backend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/PersonalTaskBoard.csproj",
        "PersonalTaskBoard/PersonalTaskBoard.sln",
        "PersonalTaskBoard/Program.cs",
        "PersonalTaskBoard/Properties/launchSettings.json",
        "PersonalTaskBoard/appsettings.json",
        "PersonalTaskBoard/appsettings.Development.json",
        ".gitignore"
      ],
      "dependencies": [],
      "estimated_hours": 2,
      "acceptance_criteria": [
        "Project targets net8.0",
        "launchSettings.json sets applicationUrl to http://localhost:5000",
        "appsettings.json has ConnectionStrings.DefaultConnection pointing to taskboard.db",
        ".gitignore excludes bin/, obj/, *.db",
        "dotnet run starts without errors"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/system-architecture-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": true,
      "requires_package_manager": ["nuget"],
      "shared_resources": ["PersonalTaskBoard/PersonalTaskBoard.csproj"],
      "read_only": false
    },
    {
      "id": "step-002",
      "title": "Domain Entities",
      "description": "Create Board, Column, TaskItem classes and Priority enum per data model spec",
      "type": "backend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/Domain/Board.cs",
        "PersonalTaskBoard/Domain/Column.cs",
        "PersonalTaskBoard/Domain/TaskItem.cs",
        "PersonalTaskBoard/Domain/Enums/Priority.cs"
      ],
      "dependencies": ["step-001"],
      "estimated_hours": 2,
      "acceptance_criteria": [
        "Board entity: Id Guid PK, Name string max 100, CreatedAt/UpdatedAt DateTimeOffset",
        "Column entity: Id, BoardId FK, Name max 80, DisplayOrder int, WipLimit int?, timestamps",
        "TaskItem entity: Id, ColumnId FK, Title max 200, Description max 4000, DueDate DateOnly?, Priority enum, AssigneeLabel max 60, SortOrder int, timestamps",
        "Priority enum: Low=0, Medium=1, High=2, Urgent=3",
        "Navigation properties present on all entities"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/data-model-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-003",
      "title": "DbContext & EF Core Configuration",
      "description": "Create TaskBoardDbContext with full model config: FK constraints, cascade deletes, unique indexes, max lengths. Add SQLite NuGet packages.",
      "type": "backend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/Data/TaskBoardDbContext.cs",
        "PersonalTaskBoard/PersonalTaskBoard.csproj",
        "PersonalTaskBoard/Program.cs"
      ],
      "dependencies": ["step-002"],
      "estimated_hours": 3,
      "acceptance_criteria": [
        "DbSet<Board>, DbSet<Column>, DbSet<TaskItem> declared",
        "OnModelCreating configures all constraints and indexes per data model spec",
        "Priority stored as int",
        "Cascade delete Board→Column→TaskItem",
        "Program.cs registers DbContext with SQLite",
        "dotnet build succeeds"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/data-model-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": true,
      "requires_package_manager": ["nuget"],
      "shared_resources": ["PersonalTaskBoard/PersonalTaskBoard.csproj"],
      "read_only": false
    },
    {
      "id": "step-004",
      "title": "EF Core Migration & Startup Seed",
      "description": "Generate InitialCreate migration, add migrate-on-startup logic, add idempotent seed for default board and 3 default columns",
      "type": "backend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/Data/Migrations/",
        "PersonalTaskBoard/Program.cs"
      ],
      "dependencies": ["step-003"],
      "estimated_hours": 3,
      "acceptance_criteria": [
        "Migration files generated for Board, Column, TaskItem tables",
        "Program.cs calls Database.Migrate() in scoped service provider before app.Run()",
        "Seed creates 'My Board' if no boards exist",
        "Seed creates To Do, In Progress, Done columns if missing",
        "Seed is idempotent: running twice doesn't duplicate data",
        "dotnet run creates taskboard.db on first run"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/data-model-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-005",
      "title": "Columns Minimal API Endpoints",
      "description": "Implement /api/columns endpoints: list, create, update, delete, reorder. Include DTOs and validation.",
      "type": "backend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/Api/ColumnsEndpoints.cs",
        "PersonalTaskBoard/Api/DTOs/ColumnDtos.cs",
        "PersonalTaskBoard/Program.cs"
      ],
      "dependencies": ["step-004"],
      "estimated_hours": 4,
      "acceptance_criteria": [
        "GET /api/columns returns columns ordered by DisplayOrder",
        "POST /api/columns creates column, returns 201",
        "PUT /api/columns/{id} updates name/WipLimit, returns 200 or 404",
        "DELETE /api/columns/{id} cascades to tasks, returns 204 or 404",
        "PATCH /api/columns/reorder recomputes DisplayOrder from submitted ID array",
        "Validation: Name required, max 80 chars, 400 on failure"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-006",
      "title": "Tasks Minimal API Endpoints",
      "description": "Implement /api/tasks endpoints: CRUD, move between columns, reorder within column, filter by columnId/priority/search.",
      "type": "backend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/Api/TasksEndpoints.cs",
        "PersonalTaskBoard/Api/DTOs/TaskDtos.cs",
        "PersonalTaskBoard/Program.cs"
      ],
      "dependencies": ["step-005"],
      "estimated_hours": 5,
      "acceptance_criteria": [
        "GET /api/tasks supports columnId, priority, search query params",
        "POST /api/tasks creates task at bottom of column, returns 201",
        "GET /api/tasks/{id} returns full detail, 404 if not found",
        "PUT /api/tasks/{id} updates all fields, 404 if not found",
        "DELETE /api/tasks/{id} removes task, returns 204",
        "PATCH /api/tasks/{id}/move moves to new column with reorder",
        "PATCH /api/tasks/reorder recomputes SortOrder for a column"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-007",
      "title": "Shared Layout & Board Index Page",
      "description": "Create _Layout.cshtml (loads SortableJS CDN, CSS, JS), Index.cshtml Kanban board with column lanes, task cards, modals for add task/column, filter bar.",
      "type": "frontend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/Pages/Shared/_Layout.cshtml",
        "PersonalTaskBoard/Pages/Shared/_ViewImports.cshtml",
        "PersonalTaskBoard/Pages/Shared/_ViewStart.cshtml",
        "PersonalTaskBoard/Pages/Index.cshtml",
        "PersonalTaskBoard/Pages/Index.cshtml.cs",
        "PersonalTaskBoard/wwwroot/css/site.css"
      ],
      "dependencies": ["step-006"],
      "estimated_hours": 5,
      "acceptance_criteria": [
        "Layout loads SortableJS from CDN",
        "Board renders columns as horizontal lanes with task count",
        "Task cards show title, priority badge, due date, assignee",
        "Overdue cards have .task-overdue class (red indicator)",
        "Due soon cards (within 3 days) have .task-due-soon class",
        "Add Column and Add Task modals present",
        "Filter bar with search input and priority dropdown",
        "CSS defines priority and overdue state classes"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/system-architecture-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-008",
      "title": "Task Detail Page",
      "description": "Create /tasks/details Razor Page for viewing and editing a single task with all fields, delete button, back-to-board link.",
      "type": "frontend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/Pages/Tasks/Details.cshtml",
        "PersonalTaskBoard/Pages/Tasks/Details.cshtml.cs"
      ],
      "dependencies": ["step-007"],
      "estimated_hours": 3,
      "acceptance_criteria": [
        "Route /tasks/details?id={taskId} resolves correctly",
        "All task fields displayed and editable",
        "Save calls PUT /api/tasks/{id}, shows feedback",
        "Delete calls DELETE /api/tasks/{id}, redirects to /",
        "Back to Board link present",
        "Overdue indicator shown"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/system-architecture-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-009",
      "title": "board.js — Board Interactivity",
      "description": "Implement board.js: fetch and render tasks, modal form submissions (add/edit task, add/rename/delete column), search/filter, due-date CSS classes.",
      "type": "frontend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/wwwroot/js/board.js"
      ],
      "dependencies": ["step-007", "step-006"],
      "estimated_hours": 5,
      "acceptance_criteria": [
        "On load, fetches GET /api/tasks and renders cards into columns",
        "Add Task modal submits POST /api/tasks, appends card without reload",
        "Edit Column modal submits PUT /api/columns/{id}",
        "Add Column modal submits POST /api/columns, appends new lane",
        "Delete Column calls DELETE /api/columns/{id}, removes lane",
        "Search filters by title in real time (client-side, case-insensitive)",
        "Priority dropdown filters cards; All option shows all",
        "Due date classes computed from Date.now() — .task-overdue and .task-due-soon applied",
        "Card click navigates to /tasks/details?id={taskId}",
        "Dismissible error banner shown on API failure"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/system-architecture-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-010",
      "title": "dragdrop.js — SortableJS Integration",
      "description": "Wire SortableJS for card drag-drop between columns, card reorder within column, and column reorder. API call on drop, revert on failure.",
      "type": "frontend",
      "priority": 1,
      "assignee": "Coder",
      "files": [
        "PersonalTaskBoard/wwwroot/js/dragdrop.js",
        "PersonalTaskBoard/wwwroot/css/site.css"
      ],
      "dependencies": ["step-009"],
      "estimated_hours": 4,
      "acceptance_criteria": [
        "All task lists are Sortable in same group",
        "Column headers are sortable for column reorder",
        "Drop to different column calls PATCH /api/tasks/{id}/move",
        "Drop in same column calls PATCH /api/tasks/reorder",
        "Column reorder calls PATCH /api/columns/reorder",
        "DOM reverts if API call fails",
        "Touch drag support enabled"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/adrs/adr-003-dragdrop-library.md"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-011",
      "title": "QA — Unit & Integration Tests",
      "description": "Write xUnit tests for Columns and Tasks API endpoints using EF Core InMemory provider. Test CRUD, move, reorder, filter, and seed idempotency.",
      "type": "qa",
      "priority": 2,
      "assignee": "QAAgent",
      "files": [
        "PersonalTaskBoard.Tests/PersonalTaskBoard.Tests.csproj",
        "PersonalTaskBoard.Tests/Api/ColumnsEndpointsTests.cs",
        "PersonalTaskBoard.Tests/Api/TasksEndpointsTests.cs",
        "PersonalTaskBoard.Tests/Domain/TaskItemTests.cs",
        "PersonalTaskBoard.sln"
      ],
      "dependencies": ["step-010", "step-008"],
      "estimated_hours": 4,
      "acceptance_criteria": [
        "xUnit tests cover happy paths for all API endpoints",
        "404 cases tested for update/delete endpoints",
        "Negative path tests: missing Name returns 400, WipLimit=0 returns 400, invalid priority string returns 400",
        "Seed idempotency test: running seed twice yields same row count",
        "Filter tests: search, priority string, columnId query params",
        "All tests pass with dotnet test",
        "EF Core in-memory SQLite connection (Data Source=:memory:) used for integration tests"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/api-contract-req-20260504-101447-e1c0b9.yaml"
      },
      "project_scope": "PersonalTaskBoard.Tests",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": ["nuget"],
      "shared_resources": ["PersonalTaskBoard.Tests/PersonalTaskBoard.Tests.csproj", "PersonalTaskBoard.sln"],
      "read_only": false
    },
    {
      "id": "step-012",
      "title": "Security Review",
      "description": "Review all implementation files for SQL injection, input validation, secrets in code, SQLite file path safety, and OWASP concerns for a local personal tool. Write findings document.",
      "type": "security",
      "priority": 2,
      "assignee": "SecurityReviewAgent",
      "files": [
        "docs/security/security-review-req-20260504-101447-e1c0b9.md"
      ],
      "dependencies": ["step-010", "step-008"],
      "estimated_hours": 2,
      "acceptance_criteria": [
        "No secrets committed to source",
        "SQLite path is relative local path",
        "All queries use EF Core parameterisation (no raw SQL interpolation)",
        "Input validation confirmed on all endpoints",
        "Security findings documented in docs/security/security-review-req-20260504-101447-e1c0b9.md",
        "No critical findings unresolved"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/system-architecture-req-20260504-101447-e1c0b9.md"
      },
      "project_scope": "docs/security",
      "requires_git_workspace": false,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": false
    },
    {
      "id": "step-013",
      "title": "Code Review & Pull Request",
      "description": "Review all implementation for architecture adherence, create PR to dev branch with feature summary and run instructions.",
      "type": "docs",
      "priority": 3,
      "assignee": "CodeReviewAgent",
      "files": [],
      "dependencies": ["step-011", "step-012"],
      "estimated_hours": 2,
      "acceptance_criteria": [
        "PR created targeting dev branch",
        "PR title: feat: Personal task board — initial implementation",
        "PR description includes architecture references and dotnet run instructions",
        "No blocking review comments",
        "Branch: feat/personal-task-board-req-20260504-101447-e1c0b9"
      ],
      "inputs": {
        "architect_spec": "docs/architecture/manifest-req-20260504-101447-e1c0b9.yaml"
      },
      "project_scope": "PersonalTaskBoard",
      "requires_git_workspace": true,
      "requires_build_system": false,
      "requires_package_manager": [],
      "shared_resources": [],
      "read_only": true
    }
  ],
  "phases": [
    {
      "phase_id": "phase-0",
      "name": "Phase 0: Architecture (Complete)",
      "task_ids": [],
      "parallelizable": false,
      "dependencies": []
    },
    {
      "phase_id": "phase-1",
      "name": "Phase 1: Project Foundation",
      "task_ids": ["step-001", "step-002", "step-003", "step-004"],
      "parallelizable": false,
      "dependencies": ["phase-0"]
    },
    {
      "phase_id": "phase-2",
      "name": "Phase 2: API Endpoints",
      "task_ids": ["step-005", "step-006"],
      "parallelizable": false,
      "dependencies": ["phase-1"]
    },
    {
      "phase_id": "phase-3",
      "name": "Phase 3: Razor Pages",
      "task_ids": ["step-007", "step-008"],
      "parallelizable": false,
      "dependencies": ["phase-2"]
    },
    {
      "phase_id": "phase-4",
      "name": "Phase 4: Frontend JavaScript",
      "task_ids": ["step-009", "step-010"],
      "parallelizable": false,
      "dependencies": ["phase-3"]
    },
    {
      "phase_id": "phase-5",
      "name": "Phase 5: QA & Security (Parallel)",
      "task_ids": ["step-011", "step-012"],
      "parallelizable": true,
      "dependencies": ["phase-4"]
    },
    {
      "phase_id": "phase-6",
      "name": "Phase 6: Code Review & PR",
      "task_ids": ["step-013"],
      "parallelizable": false,
      "dependencies": ["phase-5"]
    }
  ],
  "total_estimated_hours": 44,
  "critical_path_hours": 42,
  "parallelization_factor": 1.05
}
```
