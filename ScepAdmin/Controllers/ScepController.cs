using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Models;
using ScepAdmin.Services;


/// <summary>
/// Implements the SCEP protocol endpoints for certificate enrollment and CA info.
/// </summary>
namespace ScepAdmin.Controllers;

/// <summary>
/// SCEP API controller: handles GetCACaps, GetCACert, and PKIOperation.
/// </summary>
[ApiController]
[Route("scep")]
public class ScepController : ControllerBase
{
    private static readonly string[] Caps = ["POSTPKIOperation", "SHA-1", "SHA-256", "AES", "DES3"];

    private readonly ICertificateService _certificateService;
    private readonly IChallengeValidationService _challengeValidationService;
    private readonly IScepRequestDecoder _requestDecoder;
    private readonly IScepCertificateFactory _certificateFactory;
    private readonly IScepResponseBuilder _responseBuilder;
    private readonly AppDbContext _db;
    private readonly ILogger<ScepController> _logger;

    /// <summary>
    /// Constructs the controller with all required services.
    /// </summary>
    public ScepController(
        ICertificateService certificateService,
        IChallengeValidationService challengeValidationService,
        IScepRequestDecoder requestDecoder,
        IScepCertificateFactory certificateFactory,
        IScepResponseBuilder responseBuilder,
        AppDbContext db,
        ILogger<ScepController> logger)
    {
        _certificateService = certificateService;
        _challengeValidationService = challengeValidationService;
        _requestDecoder = requestDecoder;
        _certificateFactory = certificateFactory;
        _responseBuilder = responseBuilder;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    /// <summary>
    /// Handles SCEP GET requests (GetCACaps, GetCACert).
    /// </summary>
    public IActionResult Get([FromQuery] string? operation)
    {
        // Return SCEP capabilities
        if (string.Equals(operation, "GetCACaps", StringComparison.OrdinalIgnoreCase))
            return Content(string.Join("\n", Caps), "text/plain");

        // Return CA certificate
        if (string.Equals(operation, "GetCACert", StringComparison.OrdinalIgnoreCase))
        {
            var cert = _certificateService.GetCaCertificate();
            if (cert == null)
                return StatusCode(503, new { status = "Unhealthy", reason = "CA certificate not loaded" });

            return File(cert.Export(X509ContentType.Cert), "application/x-x509-ca-cert", "ca.cer");
        }

        // Unknown operation
        return BadRequest(new { message = "Unsupported operation" });
    }

    [HttpPost]
    /// <summary>
    /// Handles SCEP POST (PKIOperation) for certificate enrollment.
    /// </summary>
    public async Task<IActionResult> Post(
        [FromQuery] string? operation,
        CancellationToken cancellationToken)
    {
        // Only accept PKIOperation
        if (!string.Equals(operation ?? "PKIOperation", "PKIOperation", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Unsupported operation");

        // Get CA cert
        var caCert = _certificateService.GetCaCertificate();
        if (caCert == null)
            return StatusCode(503, "CA certificate not loaded");

        // Read request body
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken);
        var requestBytes = ms.ToArray();

        _logger.LogDebug("[SCEP] Received {Bytes} bytes", requestBytes.Length);

        try
        {
            // Decode SCEP request
            var request = _requestDecoder.Decode(requestBytes, caCert);
            _logger.LogDebug("[SCEP] transactionId={TransactionId}", request.TransactionId);

            // Validate challenge and get company
            var companyId = await _challengeValidationService.GetCompanyIdAsync(request.ChallengePassword, cancellationToken);
            if (companyId is null)
            {
                _logger.LogWarning("[SCEP] Invalid challenge password — returning FAILURE");
                return File(
                    _responseBuilder.BuildFailure(caCert, request.TransactionId, request.SenderNonce, failInfo: 2),
                    "application/x-pki-message");
            }

            // Issue certificate
            var certDer = _certificateFactory.Build(caCert, request.CsrBytes);
            _logger.LogDebug("[SCEP] Issued certificate ({Bytes} bytes)", certDer.Length);

            // Persist issuance to DB
            await PersistIssuanceAsync(companyId.Value, certDer,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                cancellationToken);

            // Build SCEP response
            var response = _responseBuilder.BuildSuccess(
                caCert, request.ClientCert, request.TransactionId, request.SenderNonce, certDer);

            return File(response, "application/x-pki-message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SCEP] Error processing PKIOperation");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Persists issued certificate and device info to the database.
    /// </summary>
    private async Task PersistIssuanceAsync(int companyId, byte[] certDer, string clientIp, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        using var issuedCert = new X509Certificate2(certDer);
        var deviceName = issuedCert.GetNameInfo(X509NameType.SimpleName, false);

        var device = await _db.Devices.FirstOrDefaultAsync(
            d => d.CompanyId == companyId && d.DeviceName == deviceName, ct);

        if (device == null)
        {
            device = new Device
            {
                CompanyId = companyId,
                DeviceName = deviceName,
                DeviceIdentifier = deviceName,
                CreatedAt = now,
                LastSeenAt = now
            };
            _db.Devices.Add(device);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            device.LastSeenAt = now;
        }

        var hasPrevious = await _db.Certificates.AnyAsync(
            c => c.DeviceId == device.Id && !c.IsRevoked, ct);

        _db.Certificates.Add(new Certificate
        {
            CompanyId = companyId,
            DeviceId = device.Id,
            SerialNumber = issuedCert.SerialNumber,
            Subject = issuedCert.Subject,
            NotBefore = issuedCert.NotBefore.ToUniversalTime(),
            NotAfter = issuedCert.NotAfter.ToUniversalTime(),
            IsRevoked = false,
            IsRenewal = hasPrevious,
            CreatedAt = now
        });

        _db.IssuanceLogs.Add(new IssuanceLog
        {
            CompanyId = companyId,
            DeviceId = device.Id,
            Operation = "Issue",
            Status = "Success",
            Message = $"SCEP certificate issued. Serial={issuedCert.SerialNumber}",
            ClientIp = clientIp,
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);
    }
}
