namespace PersonalTaskBoard.Api;

public record TaskDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    string? Description,
    DateOnly? DueDate,
    string Priority,
    string? AssigneeLabel,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record TaskSummaryDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    DateOnly? DueDate,
    string Priority,
    string? AssigneeLabel,
    int SortOrder);

public record CreateTaskRequest(
    Guid ColumnId,
    string Title,
    string? Description,
    DateOnly? DueDate,
    string? Priority,
    string? AssigneeLabel);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    DateOnly? DueDate,
    string? Priority,
    string? AssigneeLabel);

public record MoveTaskRequest(Guid TargetColumnId, int TargetIndex);

public record ReorderTasksRequest(Guid ColumnId, List<Guid> OrderedIds);
