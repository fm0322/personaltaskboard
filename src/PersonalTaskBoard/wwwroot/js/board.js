(function () {
    'use strict';

    // ── Helpers ──────────────────────────────────────────────────────────────

    function showError(msg) {
        const banner = document.getElementById('error-banner');
        document.getElementById('error-message').textContent = msg;
        banner.style.display = 'flex';
        setTimeout(() => banner.style.display = 'none', 5000);
    }

    function priorityClass(priority) {
        return 'priority-' + (priority || 'medium').toLowerCase();
    }

    function dueDateClass(dueDateStr) {
        if (!dueDateStr) return '';
        const due = new Date(dueDateStr + 'T00:00:00');
        const now = new Date();
        now.setHours(0, 0, 0, 0);
        const diff = (due - now) / (1000 * 60 * 60 * 24);
        if (diff < 0) return 'task-overdue';
        if (diff <= 3) return 'task-due-soon';
        return '';
    }

    function formatDate(dateStr) {
        if (!dateStr) return '';
        const [y, m, d] = dateStr.split('-');
        return `${m}/${d}/${y}`;
    }

    // ── Card Rendering ────────────────────────────────────────────────────────

    function createTaskCard(task) {
        const li = document.createElement('li');
        li.className = `task-card ${priorityClass(task.priority)} ${dueDateClass(task.dueDate)}`;
        li.dataset.taskId = task.id;
        li.dataset.priority = (task.priority || 'Medium').toLowerCase();
        li.dataset.title = (task.title || '').toLowerCase();

        li.innerHTML = `
            <div class="task-card-title">${escapeHtml(task.title)}</div>
            <div class="task-card-meta">
                <span class="priority-badge ${priorityClass(task.priority)}">${task.priority || 'Medium'}</span>
                ${task.dueDate ? `<span class="task-due">${formatDate(task.dueDate)}</span>` : ''}
                ${task.assigneeLabel ? `<span class="task-assignee">👤 ${escapeHtml(task.assigneeLabel)}</span>` : ''}
            </div>
        `;

        li.addEventListener('click', () => {
            window.location.href = `/tasks/details?id=${task.id}`;
        });

        return li;
    }

    function escapeHtml(str) {
        const d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

    // ── Load Board ────────────────────────────────────────────────────────────

    async function loadBoard() {
        const boardId = document.getElementById('board-id')?.value;
        if (!boardId) return;

        // Load all columns for this board, then load tasks per column
        try {
            const colRes = await fetch(`/api/columns?boardId=${boardId}`);
            if (!colRes.ok) throw new Error('Failed to load columns');
            const columns = await colRes.json();

            // Load tasks for all columns in parallel
            await Promise.all(columns.map(col => loadColumnTasks(col.id)));
        } catch (e) {
            showError('Failed to load board: ' + e.message);
        }
    }

    async function loadColumnTasks(columnId) {
        try {
            const res = await fetch(`/api/tasks?columnId=${columnId}`);
            if (!res.ok) throw new Error('Failed to load tasks');
            const tasks = await res.json();
            const list = document.querySelector(`.task-list[data-column-id="${columnId}"]`);
            if (!list) return;
            list.innerHTML = '';
            tasks.forEach(task => list.appendChild(createTaskCard(task)));
        } catch (e) {
            showError('Failed to load tasks for column');
        }
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    function applyFilters() {
        const search = (document.getElementById('search-input')?.value || '').toLowerCase();
        const priority = (document.getElementById('priority-filter')?.value || '').toLowerCase();

        document.querySelectorAll('.task-card').forEach(card => {
            const titleMatch = !search || card.dataset.title.includes(search);
            const priorityMatch = !priority || card.dataset.priority === priority;
            card.style.display = (titleMatch && priorityMatch) ? '' : 'none';
        });
    }

    document.getElementById('search-input')?.addEventListener('input', applyFilters);
    document.getElementById('priority-filter')?.addEventListener('change', applyFilters);

    // ── Column Modal ──────────────────────────────────────────────────────────

    let editingColumnId = null;

    function openColumnModal(mode, columnId, name, wipLimit) {
        editingColumnId = mode === 'edit' ? columnId : null;
        document.getElementById('column-modal-title').textContent = mode === 'edit' ? 'Edit Column' : 'Add Column';
        document.getElementById('column-name-input').value = name || '';
        document.getElementById('column-wip-input').value = wipLimit || '';
        document.getElementById('column-modal').style.display = 'flex';
        document.getElementById('column-name-input').focus();
    }

    function closeColumnModal() {
        document.getElementById('column-modal').style.display = 'none';
        editingColumnId = null;
    }

    document.getElementById('add-column-btn')?.addEventListener('click', () => openColumnModal('add'));
    document.getElementById('column-modal-cancel')?.addEventListener('click', closeColumnModal);

    document.querySelectorAll('.edit-column-btn').forEach(btn => {
        btn.addEventListener('click', e => {
            e.stopPropagation();
            openColumnModal('edit', btn.dataset.columnId, btn.dataset.columnName, btn.dataset.wipLimit);
        });
    });

    document.getElementById('column-modal-save')?.addEventListener('click', async () => {
        const name = document.getElementById('column-name-input').value.trim();
        const wipRaw = document.getElementById('column-wip-input').value.trim();
        const wipLimit = wipRaw ? parseInt(wipRaw, 10) : null;

        if (!name) { showError('Column name is required'); return; }
        if (wipRaw && (isNaN(wipLimit) || wipLimit < 1)) { showError('WIP limit must be 1 or greater'); return; }

        const boardId = document.getElementById('board-id')?.value;

        try {
            let res;
            if (editingColumnId) {
                res = await fetch(`/api/columns/${editingColumnId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ name, wipLimit })
                });
                if (res.ok) {
                    const col = document.getElementById(`col-${editingColumnId}`);
                    if (col) {
                        col.querySelector('.column-title').textContent = name;
                        const wipEl = col.querySelector('.wip-limit');
                        if (wipLimit) {
                            if (wipEl) { wipEl.textContent = `WIP: ${wipLimit}`; wipEl.dataset.wip = wipLimit; }
                        } else {
                            wipEl?.remove();
                        }
                        col.querySelector('.edit-column-btn').dataset.columnName = name;
                        col.querySelector('.edit-column-btn').dataset.wipLimit = wipLimit || '';
                    }
                }
            } else {
                res = await fetch('/api/columns', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ boardId, name, wipLimit })
                });
                if (res.ok || res.status === 201) {
                    const newCol = await res.json();
                    appendColumnLane(newCol);
                }
            }
            if (!res.ok && res.status !== 201) {
                const err = await res.json().catch(() => ({}));
                showError(err.message || 'Failed to save column');
                return;
            }
            closeColumnModal();
        } catch (e) {
            showError('Network error: ' + e.message);
        }
    });

    function appendColumnLane(col) {
        const board = document.getElementById('board-container');
        const div = document.createElement('div');
        div.className = 'column-lane';
        div.dataset.columnId = col.id;
        div.id = `col-${col.id}`;
        div.innerHTML = `
            <div class="column-header">
                <span class="column-title">${escapeHtml(col.name)}</span>
                ${col.wipLimit ? `<span class="wip-limit" data-wip="${col.wipLimit}">WIP: ${col.wipLimit}</span>` : ''}
                <div class="column-actions">
                    <button class="btn-icon edit-column-btn" data-column-id="${col.id}" data-column-name="${escapeHtml(col.name)}" data-wip-limit="${col.wipLimit || ''}" title="Edit column">✏️</button>
                    <button class="btn-icon delete-column-btn" data-column-id="${col.id}" data-column-name="${escapeHtml(col.name)}" title="Delete column">🗑️</button>
                </div>
            </div>
            <ul class="task-list" data-column-id="${col.id}" id="task-list-${col.id}"></ul>
            <button class="btn btn-add-task" data-column-id="${col.id}">+ Add Task</button>
        `;
        board.appendChild(div);
        // wire up new buttons
        div.querySelector('.edit-column-btn').addEventListener('click', e => {
            e.stopPropagation();
            const btn = e.currentTarget;
            openColumnModal('edit', btn.dataset.columnId, btn.dataset.columnName, btn.dataset.wipLimit);
        });
        div.querySelector('.delete-column-btn').addEventListener('click', e => {
            e.stopPropagation();
            handleDeleteColumn(e.currentTarget);
        });
        div.querySelector('.btn-add-task').addEventListener('click', e => {
            openTaskModal(e.currentTarget.dataset.columnId);
        });
        // register with SortableJS (dragdrop.js exposes window.initColumnSortable)
        if (typeof window.initColumnSortable === 'function') {
            window.initColumnSortable(div.querySelector('.task-list'));
        }
    }

    // ── Delete Column ─────────────────────────────────────────────────────────

    async function handleDeleteColumn(btn) {
        const columnId = btn.dataset.columnId;
        const columnName = btn.dataset.columnName;
        if (!confirm(`Delete column "${columnName}" and all its tasks?`)) return;
        try {
            const res = await fetch(`/api/columns/${columnId}`, { method: 'DELETE' });
            if (res.ok || res.status === 204) {
                document.getElementById(`col-${columnId}`)?.remove();
            } else {
                const err = await res.json().catch(() => ({}));
                showError(err.message || 'Failed to delete column');
            }
        } catch (e) {
            showError('Network error: ' + e.message);
        }
    }

    document.querySelectorAll('.delete-column-btn').forEach(btn => {
        btn.addEventListener('click', e => { e.stopPropagation(); handleDeleteColumn(btn); });
    });

    // ── Task Modal ────────────────────────────────────────────────────────────

    function openTaskModal(columnId) {
        document.getElementById('task-modal-column-id').value = columnId;
        document.getElementById('task-title-input').value = '';
        document.getElementById('task-desc-input').value = '';
        document.getElementById('task-priority-input').value = 'Medium';
        document.getElementById('task-due-input').value = '';
        document.getElementById('task-assignee-input').value = '';
        document.getElementById('task-modal').style.display = 'flex';
        document.getElementById('task-title-input').focus();
    }

    function closeTaskModal() {
        document.getElementById('task-modal').style.display = 'none';
    }

    document.querySelectorAll('.btn-add-task').forEach(btn => {
        btn.addEventListener('click', () => openTaskModal(btn.dataset.columnId));
    });
    document.getElementById('task-modal-cancel')?.addEventListener('click', closeTaskModal);

    document.getElementById('task-modal-save')?.addEventListener('click', async () => {
        const columnId = document.getElementById('task-modal-column-id').value;
        const title = document.getElementById('task-title-input').value.trim();
        if (!title) { showError('Task title is required'); return; }

        const body = {
            columnId,
            title,
            description: document.getElementById('task-desc-input').value.trim() || null,
            priority: document.getElementById('task-priority-input').value,
            dueDate: document.getElementById('task-due-input').value || null,
            assigneeLabel: document.getElementById('task-assignee-input').value.trim() || null
        };

        try {
            const res = await fetch('/api/tasks', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (res.ok || res.status === 201) {
                const newTask = await res.json();
                const list = document.querySelector(`.task-list[data-column-id="${columnId}"]`);
                if (list) list.appendChild(createTaskCard(newTask));
                closeTaskModal();
            } else {
                const err = await res.json().catch(() => ({}));
                showError(err.message || 'Failed to create task');
            }
        } catch (e) {
            showError('Network error: ' + e.message);
        }
    });

    // ── Close modals on overlay click ─────────────────────────────────────────

    document.getElementById('column-modal')?.addEventListener('click', e => {
        if (e.target === e.currentTarget) closeColumnModal();
    });
    document.getElementById('task-modal')?.addEventListener('click', e => {
        if (e.target === e.currentTarget) closeTaskModal();
    });

    // ── Init ──────────────────────────────────────────────────────────────────

    window.addEventListener('DOMContentLoaded', loadBoard);

    // Expose for dragdrop.js to call after DOM mutations
    window.boardJs = { loadColumnTasks, createTaskCard, showError };

})();
