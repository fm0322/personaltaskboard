using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalTaskBoard.Data;

namespace PersonalTaskBoard.Tests.Helpers;

public static class TestDbHelper
{
    public static TaskBoardDbContext CreateInMemoryDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TaskBoardDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new TaskBoardDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
