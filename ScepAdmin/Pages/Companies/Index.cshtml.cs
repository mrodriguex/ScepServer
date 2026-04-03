using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;

namespace ScepAdmin.Pages.Companies;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Company> Companies { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Companies = await _db.Companies
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string challengePassword, string intermediateCertPath, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(challengePassword))
        {
            StatusMessage = "Name and challenge are required.";
            return RedirectToPage();
        }

        _db.Companies.Add(new Company
        {
            Name = name.Trim(),
            ChallengePassword = challengePassword,
            IntermediateCertPath = intermediateCertPath?.Trim() ?? string.Empty,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        StatusMessage = "Company created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string name, string challengePassword, string intermediateCertPath, bool isActive)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (company == null)
        {
            StatusMessage = "Company not found.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(challengePassword))
        {
            StatusMessage = "Name and challenge are required.";
            return RedirectToPage();
        }

        company.Name = name.Trim();
        company.ChallengePassword = challengePassword;
        company.IntermediateCertPath = intermediateCertPath?.Trim() ?? string.Empty;
        company.IsActive = isActive;

        await _db.SaveChangesAsync();
        StatusMessage = "Company updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (company == null)
        {
            StatusMessage = "Company not found.";
            return RedirectToPage();
        }

        var hasRelations = await _db.Devices.AnyAsync(d => d.CompanyId == id) || await _db.Certificates.AnyAsync(c => c.CompanyId == id);
        if (hasRelations)
        {
            StatusMessage = "Cannot delete company with related devices/certificates.";
            return RedirectToPage();
        }

        _db.Companies.Remove(company);
        await _db.SaveChangesAsync();

        StatusMessage = "Company deleted.";
        return RedirectToPage();
    }
}
