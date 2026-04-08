using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;


/// <summary>
/// Seeds demo data (company, device, certificates) for development/testing.
/// </summary>
namespace ScepAdmin.Services;

/// <summary>
/// Service for seeding demo data into the database.
/// </summary>
public class BootstrapService : IBootstrapService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Constructs the service with a database context.
    /// </summary>
    public BootstrapService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Seeds demo company, device, and certificates if the DB is empty.
    /// </summary>
    public async Task<object> SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        // Only seed if DB is empty
        var hasAnyCompany = await _db.Companies.AnyAsync(cancellationToken);
        var hasAnyDevice = await _db.Devices.AnyAsync(cancellationToken);
        var hasAnyCertificate = await _db.Certificates.AnyAsync(cancellationToken);

        if (hasAnyCompany || hasAnyDevice || hasAnyCertificate)
        {
            return new
            {
                seeded = false,
                message = "Skipped: data already exists"
            };
        }

        var now = DateTime.UtcNow;

        // Create demo company
        var company = new Company
        {
            Name = "Acme Corp",
            ChallengePassword = "acme-challenge",
            IntermediateCertPath = string.Empty,
            IsActive = true,
            CreatedAt = now
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);

        // Create demo device
        var device = new Device
        {
            CompanyId = company.Id,
            DeviceName = "LAPTOP-001",
            DeviceIdentifier = "DEV-001",
            LastSeenAt = now,
            CreatedAt = now
        };
        _db.Devices.Add(device);
        await _db.SaveChangesAsync(cancellationToken);

        // Add demo certificates (active, expiring, revoked)
        _db.Certificates.AddRange(
            new Certificate
            {
                CompanyId = company.Id,
                DeviceId = device.Id,
                SerialNumber = "SER-ACT-001",
                Subject = "CN=LAPTOP-001,O=Acme Corp",
                NotBefore = now.AddDays(-20),
                NotAfter = now.AddDays(30),
                IsRevoked = false,
                IsRenewal = false,
                CreatedAt = now.AddDays(-20)
            },
            new Certificate
            {
                CompanyId = company.Id,
                DeviceId = device.Id,
                SerialNumber = "SER-EXP-001",
                Subject = "CN=LAPTOP-001,O=Acme Corp",
                NotBefore = now.AddDays(-20),
                NotAfter = now.AddDays(3),
                IsRevoked = false,
                IsRenewal = true,
                CreatedAt = now.AddDays(-20)
            },
            new Certificate
            {
                CompanyId = company.Id,
                DeviceId = device.Id,
                SerialNumber = "SER-REV-001",
                Subject = "CN=LAPTOP-001,O=Acme Corp",
                NotBefore = now.AddDays(-30),
                NotAfter = now.AddDays(60),
                IsRevoked = true,
                RevokedAt = now.AddDays(-1),
                IsRenewal = false,
                CreatedAt = now.AddDays(-30)
            }
        );

        _db.IssuanceLogs.Add(new IssuanceLog
        {
            CompanyId = company.Id,
            DeviceId = device.Id,
            Operation = "Bootstrap",
            Status = "Success",
            Message = "Demo data seeded",
            ClientIp = "system",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new
        {
            seeded = true,
            companyId = company.Id,
            deviceId = device.Id,
            message = "Demo data seeded"
        };
    }
}
