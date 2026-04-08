using Microsoft.AspNetCore.Mvc;
using ScepAdmin.Services;

namespace ScepAdmin.Controllers;

/// <summary>
/// API endpoints for certificate revocation and CRL operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly ICertificateIssuanceService _issuanceService;
    private readonly ICrlService _crlService;

    /// <summary>
    /// Constructs the controller with required services.
    /// </summary>
    public OperationsController(ICertificateIssuanceService issuanceService, ICrlService crlService)
    {
        _issuanceService = issuanceService;
        _crlService = crlService;
    }

    /// <summary>
    /// Revokes a certificate by serial number.
    /// </summary>
    /// <param name="request">Revocation request with serial and reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        // Get client IP for audit log
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _issuanceService.RevokeBySerialAsync(request.SerialNumber, request.Reason, clientIp, cancellationToken);
        // Return 200 OK if success, 400 BadRequest otherwise
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Generates a new CRL (certificate revocation list).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("crl/generate")]
    public async Task<IActionResult> GenerateCrl(CancellationToken cancellationToken)
    {
        var result = await _crlService.GenerateAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the status of the most recent CRL generation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("crl/status")]
    public async Task<IActionResult> CrlStatus(CancellationToken cancellationToken)
    {
        var status = await _crlService.GetStatusAsync(cancellationToken);
        if (status == null)
        {
            // No CRL generated yet
            return Ok(new { message = "No CRL generated yet" });
        }
        return Ok(status);
    }

    /// <summary>
    /// Request body for certificate revocation.
    /// </summary>
    public sealed class RevokeRequest
    {
        /// <summary>Serial number of the certificate to revoke.</summary>
        public string SerialNumber { get; set; } = string.Empty;
        /// <summary>Reason for revocation.</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
