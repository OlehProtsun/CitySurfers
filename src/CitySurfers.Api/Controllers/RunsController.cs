using CitySurfers.Api.Contracts;
using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;
using Microsoft.AspNetCore.Mvc;

namespace CitySurfers.Api.Controllers;

[ApiController]
[Route("api/runs")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public sealed class RunsController(RunSessionService runs) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RunResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RunResponse>> Start(CancellationToken cancellationToken)
    {
        var result = await runs.StartAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { runId = result.Run.Id }, RunResponse.From(result));
    }

    [HttpGet("active")]
    [ProducesResponseType<RunResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RunResponse>> GetActive(CancellationToken cancellationToken) =>
        Ok(RunResponse.From(await runs.GetActiveAsync(cancellationToken)));

    [HttpGet("{runId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(string runId, CancellationToken cancellationToken)
    {
        var result = await runs.GetByIdAsync(runId, cancellationToken);
        return result.Run.Status == RunStatus.Completed
            ? Ok(PostRunSummary.From(result)) : Ok(RunResponse.From(result));
    }

    [HttpPatch("{runId}/progress")]
    [ProducesResponseType<RunResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RunResponse>> Progress(string runId, RunProgressInput input,
        CancellationToken cancellationToken) => Ok(RunResponse.From(await runs.UpdateProgressAsync(
            runId, input.DistanceMeters!.Value, input.DurationSeconds!.Value, cancellationToken)));

    [HttpPost("{runId}/finish")]
    [ProducesResponseType<PostRunSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostRunSummary>> Finish(string runId, RunProgressInput input,
        CancellationToken cancellationToken) => Ok(PostRunSummary.From(await runs.FinishAsync(
            runId, input.DistanceMeters!.Value, input.DurationSeconds!.Value, cancellationToken)));
}
