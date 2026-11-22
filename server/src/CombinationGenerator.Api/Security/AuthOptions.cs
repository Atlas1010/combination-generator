using System.ComponentModel.DataAnnotations;

namespace CombinationGenerator.Api.Security;

public enum AuthMode
{
    GuestTokens,
    Oidc,
}

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public AuthMode Mode { get; init; } = AuthMode.GuestTokens;

    [Required]
    public string Audience { get; init; } = "combination-generator-api";

    public string? Authority { get; init; }

    public string GuestIssuer { get; init; } = "combination-generator-api";

    public string? GuestSigningKey { get; init; }

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan GuestTokenLifetime { get; init; } = TimeSpan.FromHours(2);

    public bool AllowGuestTokensOutsideDevelopment { get; init; }
}
