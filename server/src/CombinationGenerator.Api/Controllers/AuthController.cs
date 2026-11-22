using CombinationGenerator.Api.Infrastructure;
using CombinationGenerator.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Api.Controllers;

// Issues anonymous guest tokens when guest-token mode is enabled.
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    // Returns a short-lived JWT identifying a new anonymous guest.
    [HttpPost("guest-token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.TokenIssuancePolicy)]
    [ProducesResponseType<GuestToken>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GuestToken> IssueGuestToken(
        [FromServices] GuestTokenIssuer issuer,
        [FromServices] IOptions<AuthOptions> options)
    {
        return options.Value.Mode == AuthMode.GuestTokens ? Ok(issuer.Issue()) : NotFound();
    }
}
