using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PersonalTaskBoard.Data;
using PersonalTaskBoard.Domain;
using PersonalTaskBoard.Domain.Enums;

namespace PersonalTaskBoard.Pages.Tasks;

public class DetailsModel : PageModel
{
    private readonly TaskBoardDbContext _db;

    public DetailsModel(TaskBoardDbContext db) => _db = db;

    public TaskItem? Task { get; private set; }
    public Column? Column { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Task = await _db.TaskItems.FindAsync(id);
        if (Task == null) return NotFound();
        Column = await _db.Columns.FindAsync(Task.ColumnId);
        return Page();
    }
}
