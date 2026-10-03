using CitySurfers.Application.Goals;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/goals")]
public sealed class GoalsController(NextGoalService goals) : ControllerBase
{
    [HttpGet("next")]
    [ProducesResponseType<NextGoalResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NextGoalResponse>> Next(CancellationToken cancellationToken) =>
        Ok(await goals.GetAsync(cancellationToken));
}
