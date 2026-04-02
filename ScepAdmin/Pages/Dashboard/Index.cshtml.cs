using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;

namespace ScepAdmin.Pages.Dashboard;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public int TotalOrganizations { get; set; }
    public int TotalDevices { get; set; }
    public int TotalCertificates { get; set; }
    public int RevokedCertificates { get; set; }
    public int ExpiringCertificates { get; set; }

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;
        var sevenDays = now.AddDays(7);

        TotalOrganizations = await _db.Companies.CountAsync();
        TotalDevices = await _db.Devices.CountAsync();
        TotalCertificates = await _db.Certificates.CountAsync();
        RevokedCertificates = await _db.Certificates.CountAsync(c => c.IsRevoked);
        ExpiringCertificates = await _db.Certificates.CountAsync(c =>
            !c.IsRevoked && c.NotAfter >= now && c.NotAfter <= sevenDays);
    }
}
