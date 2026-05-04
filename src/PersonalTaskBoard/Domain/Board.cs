namespace PersonalTaskBoard.Domain;

public class Board
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Column> Columns { get; set; } = new List<Column>();
}
