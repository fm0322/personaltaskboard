namespace PersonalTaskBoard.Domain;

public class Column
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int? WipLimit { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Board Board { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
