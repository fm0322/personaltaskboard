(function () {
    'use strict';

    if (typeof Sortable === 'undefined') {
        console.warn('SortableJS not loaded — drag and drop disabled');
        return;
    }

    // ── State snapshot for revert ─────────────────────────────────────────────

    function snapshotOrder(list) {
        return Array.from(list.children).map(el => el.dataset.taskId || el.dataset.columnId);
    }

    function revertOrder(list, snapshot, idAttr) {
        const byId = {};
        Array.from(list.children).forEach(el => byId[el.dataset[idAttr]] = el);
        snapshot.forEach(id => {
            const el = byId[id];
            if (el) list.appendChild(el);
        });
    }

    // ── Task list sortable ────────────────────────────────────────────────────

    function initTaskList(listEl) {
        return Sortable.create(listEl, {
            group: 'tasks',
            animation: 150,
            ghostClass: 'sortable-ghost',
            dragClass: 'sortable-drag',
            handle: '.task-card',
            forceFallback: false,
            onEnd: async function (evt) {
                const taskId = evt.item.dataset.taskId;
                const fromList = evt.from;
                const toList = evt.to;
                const fromColumnId = fromList.dataset.columnId;
                const toColumnId = toList.dataset.columnId;
                const newIndex = evt.newIndex;

                if (fromColumnId === toColumnId) {
                    // Same-column reorder
                    const orderedIds = Array.from(toList.children)
                        .map(el => el.dataset.taskId)
                        .filter(Boolean);
                    const snapshot = [...orderedIds]; // already applied to DOM
                    try {
                        const res = await fetch('/api/tasks/reorder', {
                            method: 'PATCH',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ columnId: fromColumnId, orderedIds })
                        });
                        if (!res.ok) {
                            revertOrder(toList, snapshot, 'taskId');
                            window.boardJs?.showError('Failed to reorder tasks');
                        }
                    } catch (e) {
                        window.boardJs?.showError('Network error during reorder');
                    }
                } else {
                    // Cross-column move
                    const fromSnapshot = snapshotOrder(fromList);
                    const toSnapshot = snapshotOrder(toList);
                    try {
                        const res = await fetch(`/api/tasks/${taskId}/move`, {
                            method: 'PATCH',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ targetColumnId: toColumnId, targetIndex: newIndex })
                        });
                        if (!res.ok) {
                            // revert: move card back to original position
                            fromList.appendChild(evt.item);
                            revertOrder(fromList, fromSnapshot, 'taskId');
                            revertOrder(toList, toSnapshot, 'taskId');
                            window.boardJs?.showError('Failed to move task');
                        }
                    } catch (e) {
                        fromList.appendChild(evt.item);
                        window.boardJs?.showError('Network error during move');
                    }
                }
            }
        });
    }

    // ── Column reordering ─────────────────────────────────────────────────────

    function initBoardContainer() {
        const container = document.getElementById('board-container');
        if (!container) return;

        Sortable.create(container, {
            animation: 150,
            ghostClass: 'sortable-ghost',
            dragClass: 'sortable-drag',
            handle: '.column-header',
            filter: '.btn-add-task, .btn-icon, input, select, button',
            preventOnFilter: true,
            onEnd: async function (evt) {
                const ids = Array.from(container.querySelectorAll('.column-lane'))
                    .map(el => el.dataset.columnId)
                    .filter(Boolean);
                const snapshot = [...ids];
                try {
                    const res = await fetch('/api/columns/reorder', {
                        method: 'PATCH',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ ids })
                    });
                    if (!res.ok) {
                        window.boardJs?.showError('Failed to reorder columns');
                    }
                } catch (e) {
                    window.boardJs?.showError('Network error during column reorder');
                }
            }
        });
    }

    // ── Public API (for dynamically added columns) ────────────────────────────

    window.initColumnSortable = initTaskList;

    // ── Init after board.js has loaded tasks ──────────────────────────────────

    window.addEventListener('DOMContentLoaded', function () {
        // board.js loads tasks async; we wait a tick to ensure lists are populated
        // but sortable works on empty lists too — just init immediately
        document.querySelectorAll('.task-list').forEach(initTaskList);
        initBoardContainer();
    });

})();
