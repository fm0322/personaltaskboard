using Microsoft.EntityFrameworkCore;
using PersonalTaskBoard.Domain;
using PersonalTaskBoard.Domain.Enums;

namespace PersonalTaskBoard.Data;

public class TaskBoardDbContext(DbContextOptions<TaskBoardDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Column> Columns => Set<Column>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasMany(x => x.Columns)
                .WithOne(x => x.Board)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Column>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(80);
            entity.Property(x => x.DisplayOrder).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasIndex(x => new { x.BoardId, x.DisplayOrder }).IsUnique();
            entity.HasIndex(x => new { x.BoardId, x.Name }).IsUnique();

            entity.HasMany(x => x.Tasks)
                .WithOne(x => x.Column)
                .HasForeignKey(x => x.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Priority).HasConversion<int>().IsRequired();
            entity.Property(x => x.AssigneeLabel).HasMaxLength(60);
            entity.Property(x => x.SortOrder).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasIndex(x => new { x.ColumnId, x.SortOrder }).IsUnique();
            entity.HasIndex(x => x.Priority);
            entity.HasIndex(x => x.Title);
        });
    }
}
