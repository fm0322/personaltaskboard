using PersonalTaskBoard.Domain.Enums;

namespace PersonalTaskBoard.Domain;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public Priority Priority { get; set; }
    public string? AssigneeLabel { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Column Column { get; set; } = null!;
}
