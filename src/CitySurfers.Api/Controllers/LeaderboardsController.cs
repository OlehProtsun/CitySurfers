using CitySurfers.Application.Leaderboards;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/leaderboards")]
public sealed class LeaderboardsController(LeaderboardService leaderboards) : ControllerBase
{
    [HttpGet("today")]
    [ProducesResponseType<LeaderboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaderboardResponse>> Today(CancellationToken cancellationToken) =>
        Ok(await leaderboards.GetAsync(LeaderboardPeriod.Today, cancellationToken));

    [HttpGet("month")]
    [ProducesResponseType<LeaderboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaderboardResponse>> Month(CancellationToken cancellationToken) =>
        Ok(await leaderboards.GetAsync(LeaderboardPeriod.Month, cancellationToken));
}
