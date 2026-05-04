# ADR-003: Use SortableJS for drag-and-drop interactions

## Title
Use SortableJS for task and column drag-and-drop behavior.

## Status
Accepted

## Context
The board requires intuitive drag/drop across columns, intra-column reorder, and column reorder. Implementation must remain lightweight, work with vanilla JS, and integrate cleanly with Razor-rendered markup and REST endpoints.

## Decision
Adopt **SortableJS** for drag/drop handling:
- One Sortable group across task lists for cross-column moves.
- Dedicated Sortable instance for column lane reorder.
- Event handlers call:
  - `PATCH /api/tasks/{id}/move`
  - `PATCH /api/tasks/reorder`
  - `PATCH /api/columns/reorder`

## Consequences
Positive:
- Mature, lightweight, framework-agnostic library.
- Strong support for cross-list drag/drop and reorder events.
- Minimal integration friction with server-rendered Razor pages.

Negative:
- External dependency lifecycle (versioning/security updates).
- Requires careful client/server ordering reconciliation to avoid drift.

## Alternatives Considered
1. **Native HTML5 Drag and Drop API**
   - Rejected: significantly more boilerplate and inconsistent UX details.
2. **Dragula**
   - Rejected: less active ecosystem fit compared to SortableJS for this use case.
3. **Interact.js**
   - Rejected: broader interaction scope than needed; more integration surface.
4. **No drag/drop (button-based move only)**
   - Rejected: poorer Kanban usability and does not satisfy intended interaction quality.

