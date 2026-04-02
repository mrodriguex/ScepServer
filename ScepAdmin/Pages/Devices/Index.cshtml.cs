using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;

namespace ScepAdmin.Pages.Devices;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Device> Devices { get; set; } = new();

    public async Task OnGetAsync()
    {
        Devices = await _db.Devices
            .Include(d => d.Company)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
