using Microsoft.AspNetCore.Mvc;
using ScepAdmin.Data;
using ScepAdmin.Services;


/// <summary>
/// Health check endpoint for DB and CA certificate status.
/// </summary>
namespace ScepAdmin.Controllers;

/// <summary>
/// API controller for health checks (DB and CA cert).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICertificateService _certService;

    /// <summary>
    /// Constructs the controller with DB and certificate service.
    /// </summary>
    public HealthController(AppDbContext db, ICertificateService certService)
    {
        _db = db;
        _certService = certService;
    }

    [HttpGet]
    /// <summary>
    /// Returns 200 if DB and CA cert are healthy, 503 otherwise.
    /// </summary>
    public async Task<IActionResult> Get()
    {
        try
        {
            await _db.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "Unhealthy", reason = "DB connection failed: " + ex.Message });
        }

        if (!_certService.IsCaLoaded)
        {
            return StatusCode(503, new { status = "Unhealthy", reason = "CA certificate not loaded" });
        }

        return Ok(new { status = "Healthy" });
    }
}
