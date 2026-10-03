using CitySurfers.Application.ActivityMap;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/map")]
public sealed class MapController(IActivityMapProvider activity) : ControllerBase
{
    [HttpGet("activity")]
    [ProducesResponseType<ActivityMapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivityMapResponse>> GetActivity(CancellationToken cancellationToken,
        [FromQuery] string? period = null)
    {
        var selected = (period ?? (Request.Query.ContainsKey(nameof(period)) ? "" : "today")) switch
        {
            "live" => ActivityPeriod.Live,
            "today" => ActivityPeriod.Today,
            "month" => ActivityPeriod.Month,
            _ => (ActivityPeriod?)null
        };
        if (selected is null)
        {
            ModelState.AddModelError(nameof(period), "Period must be live, today, or month.");
            return ValidationProblem(ModelState);
        }
        return Ok(await activity.GetAsync(selected.Value, cancellationToken));
    }
}
