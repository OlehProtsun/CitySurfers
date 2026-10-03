using System.ComponentModel.DataAnnotations;
using CitySurfers.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResult>> Login(LoginInput input, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(new LoginRequest(input.Username, input.Password), cancellationToken);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid username or password.")
            : Ok(result);
    }
}

public sealed record LoginInput(
    [Required, StringLength(100)] string Username,
    [Required, StringLength(200)] string Password);
