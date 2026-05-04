using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalTaskBoard.Data;
using PersonalTaskBoard.Domain;

namespace PersonalTaskBoard.Pages;

public class IndexModel : PageModel
{
    private readonly TaskBoardDbContext _db;

    public IndexModel(TaskBoardDbContext db) => _db = db;

    public Board? Board { get; private set; }
    public List<Column> Columns { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Board = await _db.Boards.FirstOrDefaultAsync();
        if (Board != null)
        {
            Columns = await _db.Columns
                .Where(c => c.BoardId == Board.Id)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }
    }
}
