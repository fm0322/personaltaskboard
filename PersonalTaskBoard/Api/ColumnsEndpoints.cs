using Microsoft.EntityFrameworkCore;
using PersonalTaskBoard.Data;
using PersonalTaskBoard.Domain;

namespace PersonalTaskBoard.Api;

public static class ColumnsEndpoints
{
    public static WebApplication MapColumnsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/columns");

        group.MapGet(string.Empty, async (Guid? boardId, TaskBoardDbContext db) =>
        {
            if (!boardId.HasValue)
            {
                return Results.BadRequest(new { message = "boardId is required." });
            }

            var columns = await db.Columns
                .AsNoTracking()
                .Where(x => x.BoardId == boardId.Value)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => ToDto(x))
                .ToListAsync();

            return Results.Ok(columns);
        });

        group.MapPost(string.Empty, async (Guid? boardId, CreateColumnRequest request, TaskBoardDbContext db) =>
        {
            var validationError = ValidateColumnNameAndWipLimit(request.Name, request.WipLimit);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            if (!boardId.HasValue)
            {
                return Results.BadRequest(new { message = "boardId is required." });
            }

            var boardExists = await db.Boards.AnyAsync(x => x.Id == boardId.Value);
            if (!boardExists)
            {
                return Results.NotFound(new { message = "Board not found." });
            }

            var maxOrder = await db.Columns
                .Where(x => x.BoardId == boardId.Value)
                .Select(x => (int?)x.DisplayOrder)
                .MaxAsync() ?? -1;

            var now = DateTimeOffset.UtcNow;
            var column = new Column
            {
                Id = Guid.NewGuid(),
                BoardId = boardId.Value,
                Name = request.Name.Trim(),
                DisplayOrder = maxOrder + 1,
                WipLimit = request.WipLimit,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Columns.Add(column);
            await db.SaveChangesAsync();

            return Results.Created($"/api/columns/{column.Id}", ToDto(column));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateColumnRequest request, TaskBoardDbContext db) =>
        {
            var validationError = ValidateColumnNameAndWipLimit(request.Name, request.WipLimit);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var column = await db.Columns.FirstOrDefaultAsync(x => x.Id == id);
            if (column is null)
            {
                return Results.NotFound(new { message = "Column not found." });
            }

            column.Name = request.Name.Trim();
            column.WipLimit = request.WipLimit;
            column.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToDto(column));
        });

        group.MapDelete("/{id:guid}", async (Guid id, TaskBoardDbContext db) =>
        {
            var column = await db.Columns.FirstOrDefaultAsync(x => x.Id == id);
            if (column is null)
            {
                return Results.NotFound(new { message = "Column not found." });
            }

            db.Columns.Remove(column);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPatch("/reorder", async (ReorderColumnsRequest request, TaskBoardDbContext db) =>
        {
            if (request.Ids is null || request.Ids.Count == 0)
            {
                return Results.BadRequest(new { message = "ids must contain at least one value." });
            }

            if (request.Ids.Count != request.Ids.Distinct().Count())
            {
                return Results.BadRequest(new { message = "ids must not contain duplicates." });
            }

            var columns = await db.Columns
                .Where(x => request.Ids.Contains(x.Id))
                .ToListAsync();

            if (columns.Count != request.Ids.Count)
            {
                return Results.NotFound(new { message = "One or more columns were not found." });
            }

            var now = DateTimeOffset.UtcNow;
            var byId = columns.ToDictionary(x => x.Id);
            var orderedColumns = request.Ids.Select(id => byId[id]).ToList();

            for (var i = 0; i < orderedColumns.Count; i++)
            {
                orderedColumns[i].DisplayOrder = -1 - i;
                orderedColumns[i].UpdatedAt = now;
            }

            await db.SaveChangesAsync();

            for (var i = 0; i < orderedColumns.Count; i++)
            {
                orderedColumns[i].DisplayOrder = i;
                orderedColumns[i].UpdatedAt = now;
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static string? ValidateColumnNameAndWipLimit(string? name, int? wipLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (name.Trim().Length > 80)
        {
            return "Name must be 80 characters or fewer.";
        }

        if (wipLimit is 0 or < 0)
        {
            return "WipLimit must be null or greater than or equal to 1.";
        }

        return null;
    }

    private static ColumnDto ToDto(Column column) =>
        new(column.Id, column.BoardId, column.Name, column.DisplayOrder, column.WipLimit, column.CreatedAt, column.UpdatedAt);
}
