using Microsoft.AspNetCore.Mvc;
using ScepAdmin.Services;

namespace ScepAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BootstrapController : ControllerBase
{
    private readonly IBootstrapService _bootstrapService;

    public BootstrapController(IBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    [HttpPost("demo")]
    public async Task<IActionResult> SeedDemo(CancellationToken cancellationToken)
    {
        var result = await _bootstrapService.SeedDemoDataAsync(cancellationToken);
        return Ok(result);
    }
}
