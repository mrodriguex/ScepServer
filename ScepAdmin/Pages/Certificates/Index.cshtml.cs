using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;
using ScepAdmin.Services;

namespace ScepAdmin.Pages.Certificates;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICertificateIssuanceService _issuanceService;

    public IndexModel(AppDbContext db, ICertificateIssuanceService issuanceService)
    {
        _db = db;
        _issuanceService = issuanceService;
    }

    public List<Certificate> Certificates { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "All";

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;
        var sevenDays = now.AddDays(7);

        var query = _db.Certificates
            .Include(c => c.Device)
            .Include(c => c.Company)
            .AsQueryable();

        query = Filter switch
        {
            "Active" => query.Where(c => !c.IsRevoked && c.NotAfter > now),
            "Expiring" => query.Where(c => !c.IsRevoked && c.NotAfter >= now && c.NotAfter <= sevenDays),
            "Revoked" => query.Where(c => c.IsRevoked),
            _ => query
        };

        Certificates = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int id)
    {
        var cert = await _db.Certificates.FindAsync(id);
        if (cert != null && !cert.IsRevoked)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _issuanceService.RevokeBySerialAsync(cert.SerialNumber, reason: "", clientIp);
        }
        return RedirectToPage(new { Filter });
    }
}
