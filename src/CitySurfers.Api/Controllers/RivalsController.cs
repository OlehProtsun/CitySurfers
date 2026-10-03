using CitySurfers.Application.Rivals;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/rivals")]
public sealed class RivalsController(RivalService rivals) : ControllerBase
{
    [HttpGet("current")]
    [ProducesResponseType<RivalResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RivalResponse>> Current(CancellationToken cancellationToken) =>
        Ok(await rivals.GetAsync(cancellationToken));
}
