using Microsoft.EntityFrameworkCore;
using PersonalTaskBoard.Data;
using PersonalTaskBoard.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<TaskBoardDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>();
    db.Database.Migrate();

    if (!db.Boards.Any())
    {
        var now = DateTimeOffset.UtcNow;
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "My Board",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Boards.Add(board);
        db.Columns.AddRange(
            new Column { Id = Guid.NewGuid(), BoardId = board.Id, Name = "To Do", DisplayOrder = 0, CreatedAt = now, UpdatedAt = now },
            new Column { Id = Guid.NewGuid(), BoardId = board.Id, Name = "In Progress", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            new Column { Id = Guid.NewGuid(), BoardId = board.Id, Name = "Done", DisplayOrder = 2, CreatedAt = now, UpdatedAt = now }
        );

        db.SaveChanges();
    }
}

app.Run();
