using Microsoft.AspNetCore.Mvc;
using PROG7312POE.Api.Services;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryService _telemetryService;

    public TelemetryController(TelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TelemetryResponse>>> GetRecent([FromQuery] int take = 50)
    {
        return await _telemetryService.GetRecentAsync(take);
    }

    [HttpPost]
    public async Task<ActionResult<TelemetryResponse>> AddTelemetry(TelemetryCreateRequest request)
    {
        var (telemetry, error) = await _telemetryService.AddTelemetryAsync(request);
        if (telemetry is null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetRecent), new { take = 1 }, telemetry);
    }

    [HttpGet("engagement-summary")]
    public async Task<ActionResult<EngagementSummary>> GetEngagementSummary()
    {
        return await _telemetryService.GetEngagementSummaryAsync();
    }
}
