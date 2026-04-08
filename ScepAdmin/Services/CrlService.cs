using System.Text;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;


/// <summary>
/// Handles CRL (certificate revocation list) generation and status tracking.
/// </summary>
namespace ScepAdmin.Services;

/// <summary>
/// Service for generating and tracking CRL status in the database.
/// </summary>
public class CrlService : ICrlService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Constructs the service with a database context.
    /// </summary>
    public CrlService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generates a new CRL, updates status, and logs the operation.
    /// </summary>
    public async Task<CrlGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        // Gather all revoked certificate serials
        var now = DateTime.UtcNow;
        var revoked = await _db.Certificates
            .AsNoTracking()
            .Where(c => c.IsRevoked)
            .OrderBy(c => c.SerialNumber)
            .Select(c => c.SerialNumber)
            .ToListAsync(cancellationToken);

        // Update or create CRL status row
        var status = await _db.CrlStatuses.FirstOrDefaultAsync(cancellationToken);
        if (status == null)
        {
            status = new CrlStatus
            {
                LastGeneratedAt = now,
                NextUpdateAt = now.AddHours(24),
                Version = 1,
                IsGenerating = false
            };
            _db.CrlStatuses.Add(status);
        }
        else
        {
            status.Version = (status.Version ?? 0) + 1;
            status.LastGeneratedAt = now;
            status.NextUpdateAt = now.AddHours(24);
            status.IsGenerating = false;
        }

        var version = status.Version ?? 1;
        var generatedAt = status.LastGeneratedAt ?? now;
        var nextUpdateAt = status.NextUpdateAt ?? now.AddHours(24);

        // Build CRL payload (not a real X.509 CRL, just a demo format)
        var payload = $"version:{version}\nlastGenerated:{generatedAt:O}\nnextUpdate:{nextUpdateAt:O}\nrevoked:{string.Join(',', revoked)}";

        // Log CRL generation
        _db.IssuanceLogs.Add(new IssuanceLog
        {
            Operation = "GenerateCRL",
            Status = "Success",
            Message = $"CRL generated. Version={version}. Revoked={revoked.Count}",
            ClientIp = "system",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new CrlGenerationResult
        {
            Success = true,
            Message = "CRL generated",
            Version = version,
            GeneratedAt = generatedAt,
            NextUpdateAt = nextUpdateAt,
            RevokedCount = revoked.Count,
            PayloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
        };
    }

    /// <summary>
    /// Returns the current CRL status row, if any.
    /// </summary>
    public Task<CrlStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return _db.CrlStatuses.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }
}
