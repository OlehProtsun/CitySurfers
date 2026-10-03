using CitySurfers.Application.Progress;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/progress")]
public sealed class ProgressController(ProgressService progress) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PersonalProgress>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonalProgress>> Get(CancellationToken cancellationToken) =>
        Ok(await progress.GetAsync(cancellationToken));
}
