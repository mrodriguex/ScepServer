using Microsoft.AspNetCore.Mvc;
using ScepAdmin.Services;


/// <summary>
/// API endpoint for seeding demo data.
/// </summary>
namespace ScepAdmin.Controllers;

/// <summary>
/// API controller for demo data seeding.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BootstrapController : ControllerBase
{
    private readonly IBootstrapService _bootstrapService;

    /// <summary>
    /// Constructs the controller with the bootstrap service.
    /// </summary>
    public BootstrapController(IBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    [HttpPost("demo")]
    /// <summary>
    /// Seeds demo data (POST /api/bootstrap/demo).
    /// </summary>
    public async Task<IActionResult> SeedDemo(CancellationToken cancellationToken)
    {
        var result = await _bootstrapService.SeedDemoDataAsync(cancellationToken);
        return Ok(result);
    }
}
