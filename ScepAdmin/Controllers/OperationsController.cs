using Microsoft.AspNetCore.Mvc;
using ScepAdmin.Services;

namespace ScepAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly ICertificateIssuanceService _issuanceService;
    private readonly ICrlService _crlService;

    public OperationsController(ICertificateIssuanceService issuanceService, ICrlService crlService)
    {
        _issuanceService = issuanceService;
        _crlService = crlService;
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _issuanceService.RevokeBySerialAsync(request.SerialNumber, request.Reason, clientIp, cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("crl/generate")]
    public async Task<IActionResult> GenerateCrl(CancellationToken cancellationToken)
    {
        var result = await _crlService.GenerateAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("crl/status")]
    public async Task<IActionResult> CrlStatus(CancellationToken cancellationToken)
    {
        var status = await _crlService.GetStatusAsync(cancellationToken);
        if (status == null)
        {
            return Ok(new { message = "No CRL generated yet" });
        }

        return Ok(status);
    }

    public sealed class RevokeRequest
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
