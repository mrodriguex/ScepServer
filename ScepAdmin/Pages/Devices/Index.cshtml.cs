using Microsoft.AspNetCore.Mvc;
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
    public List<Company> Companies { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Companies = await _db.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        Devices = await _db.Devices
            .Include(d => d.Company)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(int companyId, string deviceName, string deviceIdentifier)
    {
        if (companyId <= 0 || string.IsNullOrWhiteSpace(deviceName) || string.IsNullOrWhiteSpace(deviceIdentifier))
        {
            StatusMessage = "Company, device name, and identifier are required.";
            return RedirectToPage();
        }

        var exists = await _db.Devices.AnyAsync(d => d.CompanyId == companyId && d.DeviceIdentifier == deviceIdentifier);
        if (exists)
        {
            StatusMessage = "A device with this identifier already exists for the selected company.";
            return RedirectToPage();
        }

        _db.Devices.Add(new Device
        {
            CompanyId = companyId,
            DeviceName = deviceName.Trim(),
            DeviceIdentifier = deviceIdentifier.Trim(),
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = null
        });

        await _db.SaveChangesAsync();
        StatusMessage = "Device created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, int companyId, string deviceName, string deviceIdentifier, DateTime? lastSeenAt)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
        {
            StatusMessage = "Device not found.";
            return RedirectToPage();
        }

        if (companyId <= 0 || string.IsNullOrWhiteSpace(deviceName) || string.IsNullOrWhiteSpace(deviceIdentifier))
        {
            StatusMessage = "Company, device name, and identifier are required.";
            return RedirectToPage();
        }

        var duplicate = await _db.Devices.AnyAsync(d =>
            d.Id != id &&
            d.CompanyId == companyId &&
            d.DeviceIdentifier == deviceIdentifier);

        if (duplicate)
        {
            StatusMessage = "Another device already uses this identifier for the selected company.";
            return RedirectToPage();
        }

        device.CompanyId = companyId;
        device.DeviceName = deviceName.Trim();
        device.DeviceIdentifier = deviceIdentifier.Trim();
        device.LastSeenAt = lastSeenAt;

        await _db.SaveChangesAsync();
        StatusMessage = "Device updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
        {
            StatusMessage = "Device not found.";
            return RedirectToPage();
        }

        var hasCertificates = await _db.Certificates.AnyAsync(c => c.DeviceId == id);
        if (hasCertificates)
        {
            StatusMessage = "Cannot delete a device with certificates.";
            return RedirectToPage();
        }

        _db.Devices.Remove(device);
        await _db.SaveChangesAsync();

        StatusMessage = "Device deleted.";
        return RedirectToPage();
    }
}
