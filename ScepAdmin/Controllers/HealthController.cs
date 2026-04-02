using Microsoft.AspNetCore.Mvc;
using ScepAdmin.Data;
using ScepAdmin.Services;

namespace ScepAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICertificateService _certService;

    public HealthController(AppDbContext db, ICertificateService certService)
    {
        _db = db;
        _certService = certService;
    }

    [HttpGet]
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
