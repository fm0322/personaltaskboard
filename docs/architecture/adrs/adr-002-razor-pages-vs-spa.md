# ADR-002: Use Razor Pages with vanilla JavaScript instead of SPA framework

## Title
Prefer ASP.NET Core Razor Pages + vanilla JavaScript for UI architecture.

## Status
Accepted

## Context
The feature requires server-rendered page composition, modal-based CRUD, drag/drop, filtering, and detail views. Scope is single-product, local development, and low operational complexity. Team constraints explicitly prefer Razor Pages and vanilla JS.

## Decision
Adopt:
- **Razor Pages** for page routing, rendering, and model binding.
- **Vanilla JavaScript** for dynamic interactions (fetch, modal orchestration, drag/drop wiring).
- **Minimal APIs** for board operations under `/api`.

## Consequences
Positive:
- Lower complexity and build tooling overhead than SPA setups.
- Fast startup and straightforward debugging in .NET tooling.
- Clear separation: server-rendered markup + focused client-side behavior.
- Fits requirement and stack constraints directly.

Negative:
- Advanced client-state patterns are more manual than in SPA frameworks.
- If interactivity grows significantly, JS modules may require stronger structure conventions.

## Alternatives Considered
1. **Blazor Server/WebAssembly**
   - Rejected: unnecessary added framework complexity for current scope.
2. **React/Vue SPA**
   - Rejected: introduces frontend build pipeline and architecture overhead not needed for this feature.
3. **Pure Razor without JS**
   - Rejected: insufficient UX for smooth drag/drop and modal workflows.

