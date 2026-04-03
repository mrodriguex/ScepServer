using System.Text;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;

namespace ScepAdmin.Services;

public class CrlService : ICrlService
{
    private readonly AppDbContext _db;

    public CrlService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CrlGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var revoked = await _db.Certificates
            .AsNoTracking()
            .Where(c => c.IsRevoked)
            .OrderBy(c => c.SerialNumber)
            .Select(c => c.SerialNumber)
            .ToListAsync(cancellationToken);

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

        var payload = $"version:{version}\nlastGenerated:{generatedAt:O}\nnextUpdate:{nextUpdateAt:O}\nrevoked:{string.Join(',', revoked)}";

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

    public Task<CrlStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return _db.CrlStatuses.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }
}
