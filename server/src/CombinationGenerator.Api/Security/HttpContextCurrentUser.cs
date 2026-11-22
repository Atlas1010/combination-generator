using CombinationGenerator.Application.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CombinationGenerator.Api.Security;

internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string Id =>
        accessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new UnauthorizedAccessException("The request has no authenticated subject.");
}
