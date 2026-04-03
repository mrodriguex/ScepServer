using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;

namespace ScepAdmin.Services;

public class CertificateIssuanceService : ICertificateIssuanceService
{
    private readonly AppDbContext _db;
    private readonly IChallengeValidationService _challengeValidationService;
    private readonly ICertificateService _certificateService;

    public CertificateIssuanceService(
        AppDbContext db,
        IChallengeValidationService challengeValidationService,
        ICertificateService certificateService)
    {
        _db = db;
        _challengeValidationService = challengeValidationService;
        _certificateService = certificateService;
    }

    public async Task<IssuanceResult> IssueAsync(
        ScepEnrollmentRequest request,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        if (!_certificateService.IsCaLoaded)
        {
            return await FailAsync("Issue", "CA certificate not loaded", request.CompanyId, null, clientIp, cancellationToken);
        }

        if (request.CompanyId <= 0 || string.IsNullOrWhiteSpace(request.DeviceIdentifier) || string.IsNullOrWhiteSpace(request.DeviceName))
        {
            return await FailAsync("Issue", "Missing required enrollment fields", request.CompanyId, null, clientIp, cancellationToken);
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId && c.IsActive, cancellationToken);
        if (company == null)
        {
            return await FailAsync("Issue", "Company not found or inactive", request.CompanyId, null, clientIp, cancellationToken);
        }

        var validChallenge = await _challengeValidationService.ValidateAsync(request.CompanyId, request.Challenge, cancellationToken);
        if (!validChallenge)
        {
            return await FailAsync("Issue", "Invalid challenge password", request.CompanyId, null, clientIp, cancellationToken);
        }

        var now = DateTime.UtcNow;

        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.CompanyId == request.CompanyId && d.DeviceIdentifier == request.DeviceIdentifier, cancellationToken);

        if (device == null)
        {
            device = new Device
            {
                CompanyId = request.CompanyId,
                DeviceIdentifier = request.DeviceIdentifier,
                DeviceName = request.DeviceName,
                CreatedAt = now,
                LastSeenAt = now
            };
            _db.Devices.Add(device);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            device.DeviceName = request.DeviceName;
            device.LastSeenAt = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var hasPrevious = await _db.Certificates.AnyAsync(c =>
            c.CompanyId == request.CompanyId &&
            c.DeviceId == device.Id &&
            !c.IsRevoked,
            cancellationToken);

        var validityDays = request.ValidityDays <= 0 ? 365 : Math.Min(request.ValidityDays, 825);
        var serial = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToUpperInvariant();

        var cert = new Certificate
        {
            CompanyId = request.CompanyId,
            DeviceId = device.Id,
            SerialNumber = serial,
            Subject = string.IsNullOrWhiteSpace(request.Subject)
                ? $"CN={request.DeviceName},O={company.Name}"
                : request.Subject,
            NotBefore = now,
            NotAfter = now.AddDays(validityDays),
            IsRevoked = false,
            RevokedAt = null,
            IsRenewal = hasPrevious,
            CreatedAt = now
        };

        _db.Certificates.Add(cert);
        await _db.SaveChangesAsync(cancellationToken);

        _db.IssuanceLogs.Add(new IssuanceLog
        {
            CompanyId = request.CompanyId,
            DeviceId = device.Id,
            Operation = "Issue",
            Status = "Success",
            Message = $"Certificate issued. Serial={serial}",
            ClientIp = clientIp,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new IssuanceResult
        {
            Success = true,
            Message = "Certificate issued",
            CertificateId = cert.Id,
            SerialNumber = serial
        };
    }

    public async Task<IssuanceResult> RevokeBySerialAsync(
        string serialNumber,
        string reason,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return new IssuanceResult { Success = false, Message = "Serial number is required" };
        }

        var cert = await _db.Certificates
            .FirstOrDefaultAsync(c => c.SerialNumber == serialNumber, cancellationToken);

        if (cert == null)
        {
            return new IssuanceResult { Success = false, Message = "Certificate not found" };
        }

        if (cert.IsRevoked)
        {
            return new IssuanceResult { Success = false, Message = "Certificate already revoked", CertificateId = cert.Id, SerialNumber = cert.SerialNumber };
        }

        cert.IsRevoked = true;
        cert.RevokedAt = DateTime.UtcNow;

        _db.IssuanceLogs.Add(new IssuanceLog
        {
            CompanyId = cert.CompanyId,
            DeviceId = cert.DeviceId,
            Operation = "Revoke",
            Status = "Success",
            Message = string.IsNullOrWhiteSpace(reason)
                ? $"Certificate revoked. Serial={cert.SerialNumber}"
                : $"Certificate revoked. Serial={cert.SerialNumber}. Reason={reason}",
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new IssuanceResult
        {
            Success = true,
            Message = "Certificate revoked",
            CertificateId = cert.Id,
            SerialNumber = cert.SerialNumber
        };
    }

    private async Task<IssuanceResult> FailAsync(
        string operation,
        string message,
        int? companyId,
        int? deviceId,
        string clientIp,
        CancellationToken cancellationToken)
    {
        _db.IssuanceLogs.Add(new IssuanceLog
        {
            CompanyId = companyId,
            DeviceId = deviceId,
            Operation = operation,
            Status = "Failed",
            Message = message,
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new IssuanceResult
        {
            Success = false,
            Message = message
        };
    }
}
