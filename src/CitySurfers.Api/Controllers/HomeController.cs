using CitySurfers.Application.Home;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/home")]
public sealed class HomeController(HomeService home) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HomeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<HomeResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await home.GetAsync(cancellationToken));
}
