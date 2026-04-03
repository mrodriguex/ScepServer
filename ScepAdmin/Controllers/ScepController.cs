using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using ScepAdmin.Services;

namespace ScepAdmin.Controllers;

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
    private readonly ILogger<ScepController> _logger;

    public ScepController(
        ICertificateService certificateService,
        IChallengeValidationService challengeValidationService,
        IScepRequestDecoder requestDecoder,
        IScepCertificateFactory certificateFactory,
        IScepResponseBuilder responseBuilder,
        ILogger<ScepController> logger)
    {
        _certificateService = certificateService;
        _challengeValidationService = challengeValidationService;
        _requestDecoder = requestDecoder;
        _certificateFactory = certificateFactory;
        _responseBuilder = responseBuilder;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string? operation)
    {
        if (string.Equals(operation, "GetCACaps", StringComparison.OrdinalIgnoreCase))
            return Content(string.Join("\n", Caps), "text/plain");

        if (string.Equals(operation, "GetCACert", StringComparison.OrdinalIgnoreCase))
        {
            var cert = _certificateService.GetCaCertificate();
            if (cert == null)
                return StatusCode(503, new { status = "Unhealthy", reason = "CA certificate not loaded" });

            return File(cert.Export(X509ContentType.Cert), "application/x-x509-ca-cert", "ca.cer");
        }

        return BadRequest(new { message = "Unsupported operation" });
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromQuery] string? operation,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(operation ?? "PKIOperation", "PKIOperation", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Unsupported operation");

        var caCert = _certificateService.GetCaCertificate();
        if (caCert == null)
            return StatusCode(503, "CA certificate not loaded");

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken);
        var requestBytes = ms.ToArray();

        _logger.LogDebug("[SCEP] Received {Bytes} bytes", requestBytes.Length);

        try
        {
            var request = _requestDecoder.Decode(requestBytes, caCert);
            _logger.LogDebug("[SCEP] transactionId={TransactionId}", request.TransactionId);

            if (!await _challengeValidationService.ValidateAnyAsync(request.ChallengePassword, cancellationToken))
            {
                _logger.LogWarning("[SCEP] Invalid challenge password — returning FAILURE");
                return File(
                    _responseBuilder.BuildFailure(caCert, request.TransactionId, request.SenderNonce, failInfo: 2),
                    "application/x-pki-message");
            }

            var certDer = _certificateFactory.Build(caCert, request.CsrBytes);
            _logger.LogDebug("[SCEP] Issued certificate ({Bytes} bytes)", certDer.Length);

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
}
