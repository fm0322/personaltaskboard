namespace PersonalTaskBoard.Api;

public record ColumnDto(
    Guid Id,
    Guid BoardId,
    string Name,
    int DisplayOrder,
    int? WipLimit,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateColumnRequest(string Name, int? WipLimit);

public record UpdateColumnRequest(string Name, int? WipLimit);

public record ReorderColumnsRequest(List<Guid> Ids);
