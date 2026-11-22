using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CombinationGenerator.Api.Security;

public sealed record GuestToken(string AccessToken, DateTimeOffset ExpiresAt);

// Issues anonymous guest JWTs, each with a fresh random subject.
public sealed class GuestTokenIssuer
{
    private const int MinimumKeyBytes = 32;

    private readonly AuthOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _credentials;

    public GuestTokenIssuer(IOptions<AuthOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        SigningKey = CreateSigningKey(_options.GuestSigningKey);
        _credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
    }

    public SymmetricSecurityKey SigningKey { get; }

    public GuestToken Issue()
    {
        var now = _timeProvider.GetUtcNow();
        var expires = now.Add(_options.GuestTokenLifetime);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.GuestIssuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, $"guest:{Guid.NewGuid():N}")]),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials,
        };

        return new GuestToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    private static SymmetricSecurityKey CreateSigningKey(string? configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(MinimumKeyBytes)) { KeyId = "ephemeral" };
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Auth:GuestSigningKey must be a Base64 string.", ex);
        }

        if (keyBytes.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException($"Auth:GuestSigningKey must be at least {MinimumKeyBytes * 8} bits.");
        }

        return new SymmetricSecurityKey(keyBytes) { KeyId = "configured" };
    }
}
