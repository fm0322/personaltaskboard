using Microsoft.EntityFrameworkCore;
using PersonalTaskBoard.Data;
using PersonalTaskBoard.Domain;
using PriorityEnum = PersonalTaskBoard.Domain.Enums.Priority;

namespace PersonalTaskBoard.Api;

public static class TasksEndpoints
{
    public static WebApplication MapTasksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks");

        group.MapGet(string.Empty, async (Guid? columnId, string? priority, string? search, TaskBoardDbContext db) =>
        {
            var query = db.TaskItems.AsNoTracking().AsQueryable();

            if (columnId.HasValue)
            {
                query = query.Where(x => x.ColumnId == columnId.Value);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                if (!TryParsePriority(priority, out var parsedPriority))
                {
                    return Results.BadRequest(new { message = "Priority must be one of: Low, Medium, High, Urgent." });
                }

                query = query.Where(x => x.Priority == parsedPriority);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                if (trimmedSearch.Length > 200)
                {
                    return Results.BadRequest(new { message = "search must be 200 characters or fewer." });
                }

                var lowerSearch = trimmedSearch.ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(lowerSearch));
            }

            query = columnId.HasValue
                ? query.OrderBy(x => x.SortOrder)
                : query.OrderBy(x => x.ColumnId).ThenBy(x => x.SortOrder);

            var tasks = await query
                .Select(x => new TaskSummaryDto(
                    x.Id,
                    x.ColumnId,
                    x.Title,
                    x.DueDate,
                    x.Priority.ToString(),
                    x.AssigneeLabel,
                    x.SortOrder))
                .ToListAsync();

            return Results.Ok(tasks);
        });

        group.MapPost(string.Empty, async (CreateTaskRequest request, TaskBoardDbContext db) =>
        {
            var validationError = ValidateTitle(request.Title);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            if (!TryGetRequestPriority(request.Priority, out var parsedPriority, out var priorityError))
            {
                return Results.BadRequest(new { message = priorityError });
            }

            var columnExists = await db.Columns.AnyAsync(x => x.Id == request.ColumnId);
            if (!columnExists)
            {
                return Results.NotFound(new { message = "Column not found." });
            }

            var maxSortOrder = await db.TaskItems
                .Where(x => x.ColumnId == request.ColumnId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? -1;

            var now = DateTimeOffset.UtcNow;
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                ColumnId = request.ColumnId,
                Title = request.Title.Trim(),
                Description = request.Description,
                DueDate = request.DueDate,
                Priority = parsedPriority,
                AssigneeLabel = request.AssigneeLabel,
                SortOrder = maxSortOrder + 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.TaskItems.Add(task);
            await db.SaveChangesAsync();

            return Results.Created($"/api/tasks/{task.Id}", ToTaskDto(task));
        });

        group.MapGet("/{id:guid}", async (Guid id, TaskBoardDbContext db) =>
        {
            var task = await db.TaskItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return task is null
                ? Results.NotFound(new { message = "Task not found." })
                : Results.Ok(ToTaskDto(task));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTaskRequest request, TaskBoardDbContext db) =>
        {
            var validationError = ValidateTitle(request.Title);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            if (!TryGetRequestPriority(request.Priority, out var parsedPriority, out var priorityError))
            {
                return Results.BadRequest(new { message = priorityError });
            }

            var task = await db.TaskItems.FirstOrDefaultAsync(x => x.Id == id);
            if (task is null)
            {
                return Results.NotFound(new { message = "Task not found." });
            }

            task.Title = request.Title.Trim();
            task.Description = request.Description;
            task.DueDate = request.DueDate;
            task.Priority = parsedPriority;
            task.AssigneeLabel = request.AssigneeLabel;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToTaskDto(task));
        });

        group.MapDelete("/{id:guid}", async (Guid id, TaskBoardDbContext db) =>
        {
            var task = await db.TaskItems.FirstOrDefaultAsync(x => x.Id == id);
            if (task is null)
            {
                return Results.NotFound(new { message = "Task not found." });
            }

            db.TaskItems.Remove(task);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPatch("/{id:guid}/move", async (Guid id, MoveTaskRequest request, TaskBoardDbContext db) =>
        {
            if (request.TargetIndex < 0)
            {
                return Results.BadRequest(new { message = "TargetIndex must be greater than or equal to 0." });
            }

            var task = await db.TaskItems.FirstOrDefaultAsync(x => x.Id == id);
            if (task is null)
            {
                return Results.NotFound(new { message = "Task not found." });
            }

            var targetColumnExists = await db.Columns.AnyAsync(x => x.Id == request.TargetColumnId);
            if (!targetColumnExists)
            {
                return Results.NotFound(new { message = "Target column not found." });
            }

            var sourceColumnId = task.ColumnId;
            var now = DateTimeOffset.UtcNow;

            if (sourceColumnId == request.TargetColumnId)
            {
                var sameColumnTasks = await db.TaskItems
                    .Where(x => x.ColumnId == sourceColumnId)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                sameColumnTasks.RemoveAll(x => x.Id == task.Id);
                var targetIndex = Math.Clamp(request.TargetIndex, 0, sameColumnTasks.Count);
                sameColumnTasks.Insert(targetIndex, task);

                await ApplyTaskOrderingAsync(db, now, sameColumnTasks);
            }
            else
            {
                var sourceTasks = await db.TaskItems
                    .Where(x => x.ColumnId == sourceColumnId)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                sourceTasks.RemoveAll(x => x.Id == task.Id);

                var targetTasks = await db.TaskItems
                    .Where(x => x.ColumnId == request.TargetColumnId)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                task.ColumnId = request.TargetColumnId;
                var targetIndex = Math.Clamp(request.TargetIndex, 0, targetTasks.Count);
                targetTasks.Insert(targetIndex, task);

                await ApplyTaskOrderingAsync(db, now, sourceTasks, targetTasks);
            }

            return Results.Ok(ToTaskDto(task));
        });

        group.MapPatch("/reorder", async (ReorderTasksRequest request, TaskBoardDbContext db) =>
        {
            if (request.OrderedIds is null)
            {
                return Results.BadRequest(new { message = "orderedIds is required." });
            }

            if (request.OrderedIds.Count != request.OrderedIds.Distinct().Count())
            {
                return Results.BadRequest(new { message = "orderedIds must not contain duplicates." });
            }

            var columnExists = await db.Columns.AnyAsync(x => x.Id == request.ColumnId);
            if (!columnExists)
            {
                return Results.NotFound(new { message = "Column not found." });
            }

            var tasks = await db.TaskItems
                .Where(x => x.ColumnId == request.ColumnId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            if (tasks.Count != request.OrderedIds.Count)
            {
                return Results.BadRequest(new { message = "orderedIds must include all task IDs in the column." });
            }

            var byId = tasks.ToDictionary(x => x.Id);
            if (request.OrderedIds.Any(id => !byId.ContainsKey(id)))
            {
                return Results.BadRequest(new { message = "orderedIds contains unknown task IDs for the column." });
            }

            var now = DateTimeOffset.UtcNow;
            var orderedTasks = request.OrderedIds.Select(id => byId[id]).ToList();
            await ApplyTaskOrderingAsync(db, now, orderedTasks);

            return Results.NoContent();
        });

        return app;
    }

    private static async Task ApplyTaskOrderingAsync(TaskBoardDbContext db, DateTimeOffset now, params IReadOnlyList<TaskItem>[] taskGroups)
    {
        foreach (var tasks in taskGroups)
        {
            for (var i = 0; i < tasks.Count; i++)
            {
                tasks[i].SortOrder = -1 - i;
                tasks[i].UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync();

        foreach (var tasks in taskGroups)
        {
            for (var i = 0; i < tasks.Count; i++)
            {
                tasks[i].SortOrder = i;
                tasks[i].UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync();
    }

    private static string? ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required.";
        }

        if (title.Trim().Length > 200)
        {
            return "Title must be 200 characters or fewer.";
        }

        return null;
    }

    private static bool TryGetRequestPriority(string? priority, out PriorityEnum parsedPriority, out string? error)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            parsedPriority = PriorityEnum.Medium;
            error = null;
            return true;
        }

        if (TryParsePriority(priority, out parsedPriority))
        {
            error = null;
            return true;
        }

        error = "Priority must be one of: Low, Medium, High, Urgent.";
        return false;
    }

    private static bool TryParsePriority(string input, out PriorityEnum priority) =>
        Enum.TryParse(input.Trim(), ignoreCase: true, out priority) &&
        Enum.IsDefined(priority);

    private static TaskDto ToTaskDto(TaskItem task) =>
        new(
            task.Id,
            task.ColumnId,
            task.Title,
            task.Description,
            task.DueDate,
            task.Priority.ToString(),
            task.AssigneeLabel,
            task.SortOrder,
            task.CreatedAt,
            task.UpdatedAt);
}

