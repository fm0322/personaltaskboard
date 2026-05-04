# Security Review — Personal Task Board
**Request ID**: req-20260504-101447-e1c0b9  
**Date**: 2026-05-04  
**Reviewer**: SecurityReviewAgent  
**Scope**: Full implementation (steps 001–010) + remediation applied in security-fix branch

## Summary
Overall posture: **PASS** (all findings resolved).  
Initial review found 1 FAIL (missing API boundary validation) and 1 WARN (no global exception handler). Both have been remediated in this branch.

## Findings

### [PASS] Item 1 — No secrets/API keys/passwords committed
**File**: `PersonalTaskBoard/appsettings.json`, all reviewed sources  
**Detail**: No hardcoded secrets or credentials found.

### [PASS] Item 2 — SQLite file path is relative (local only)
**File**: `PersonalTaskBoard/appsettings.json`  
**Detail**: Connection string uses `Data Source=taskboard.db` (relative local file path, no network exposure).

### [PASS] Item 3 — No SQL injection vectors
**File**: `PersonalTaskBoard/Api/ColumnsEndpoints.cs`, `PersonalTaskBoard/Api/TasksEndpoints.cs`  
**Detail**: All data access uses EF Core LINQ. No raw SQL concatenation or `FromSqlRaw`/`ExecuteSqlRaw` found.

### [PASS] Item 4 — Input validation on all API endpoints (REMEDIATED)
**File**: `PersonalTaskBoard/Api/TasksEndpoints.cs`  
**Initial finding**: Description max length (4000) and AssigneeLabel max length (60) were not validated at the API boundary.  
**Remediation**: Added `ValidateOptionalFields()` helper; wired into both POST and PUT task handlers. Returns HTTP 400 with a clear message when limits are exceeded.

### [PASS] Item 5 — No dangerous file operations from user input
**File**: `Program.cs`, all API/Pages/JS files  
**Detail**: No user-driven file system operations present.

### [PASS] Item 6 — XSS protections in Razor + JS
**File**: `Pages/Index.cshtml`, `Pages/Tasks/Details.cshtml`, `wwwroot/js/board.js`  
**Detail**: Razor uses encoded `@` output. Dynamic JS rendering uses `escapeHtml()` for all user-controlled content inserted via `innerHTML`.

### [PASS] Item 7 — SortableJS CDN pinned + SRI integrity hash
**File**: `Pages/Shared/_Layout.cshtml`  
**Detail**: CDN URL pinned to `sortablejs@1.15.2` with `integrity` + `crossorigin` attributes. `@latest` not used.

### [PASS] Item 8 — No sensitive data in error responses (REMEDIATED)
**File**: `Program.cs`  
**Initial finding**: No global exception shaping; unhandled exceptions could expose stack traces.  
**Remediation**: Added centralized middleware in `Program.cs` that catches unhandled exceptions, logs them server-side, and returns a sanitized `{ message: "An unexpected error occurred." }` 500 response.

### [PASS] Item 9 — No permissive CORS policy
**File**: `Program.cs`  
**Detail**: No `AddCors`/`UseCors` configured. App is localhost-only.

### [PASS] Item 10 — Authentication (out of scope)
**Detail**: Single-user localhost app. Authentication intentionally out of scope.

## Critical Findings
None — all findings remediated.

## Conclusion
Security posture is **acceptable for a local personal tool**. All 10 checklist items pass after remediation. No blockers remain.
