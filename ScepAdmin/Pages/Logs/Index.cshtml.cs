using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;

namespace ScepAdmin.Pages.Logs;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<IssuanceLog> Logs { get; set; } = new();

    public async Task OnGetAsync()
    {
        Logs = await _db.IssuanceLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(500)
            .ToListAsync();
    }
}
