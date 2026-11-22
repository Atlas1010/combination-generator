using CombinationGenerator.Api.Contracts;
using CombinationGenerator.Api.Infrastructure;
using CombinationGenerator.Application.Combinations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CombinationGenerator.Api.Controllers;

// Exposes the Start, GetNext and GetAll APIs for the caller's sessions.
[ApiController]
[Authorize]
[Route("api/combinations")]
[EnableRateLimiting(RateLimitingSetup.ApiPolicy)]
public sealed class CombinationsController(ICombinationGeneratorService service) : ControllerBase
{
    // StartAPI: opens a session for n numbers.
    [HttpPost("start")]
    [ProducesResponseType<StartResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartResponse>> Start(StartRequest request, CancellationToken cancellationToken)
    {
        var result = await service.StartAsync(request.N, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.ToResponse());
    }

    // GetNextAPI: returns the next combination and its ordinal.
    [HttpPost("{sessionId:guid}/next")]
    [ProducesResponseType<NextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NextResponse>> GetNext(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await service.GetNextAsync(sessionId, cancellationToken);
        return Ok(result.ToResponse());
    }

    // GetAllAPI: returns one page of combinations after the anchor.
    [HttpPost("{sessionId:guid}/all")]
    [ProducesResponseType<PageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResponse>> GetAll(Guid sessionId, GetAllRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetPageAsync(sessionId, request.ToQuery(), cancellationToken);
        return Ok(result.ToResponse());
    }
}
