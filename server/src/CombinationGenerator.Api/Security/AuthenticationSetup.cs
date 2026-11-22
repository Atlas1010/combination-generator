using CombinationGenerator.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace CombinationGenerator.Api.Security;

internal static class AuthenticationSetup
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(AuthOptions.SectionName);
        var options = section.Get<AuthOptions>() ?? new AuthOptions();
        Validate(options, environment);

        services.AddOptions<AuthOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<GuestTokenIssuer>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<GuestTokenIssuer>((jwt, guestIssuer) => ConfigureJwtBearer(jwt, options, guestIssuer));

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions jwt, AuthOptions options, GuestTokenIssuer guestIssuer)
    {
        jwt.MapInboundClaims = false;
        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
        };

        if (options.Mode == AuthMode.Oidc)
        {
            jwt.Authority = options.Authority;
            jwt.RequireHttpsMetadata = true;
            jwt.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256];
        }
        else
        {
            jwt.TokenValidationParameters.ValidIssuer = options.GuestIssuer;
            jwt.TokenValidationParameters.IssuerSigningKey = guestIssuer.SigningKey;
            jwt.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
        }
    }

    // Fails startup on an unsafe or incomplete auth configuration.
    private static void Validate(AuthOptions options, IHostEnvironment environment)
    {
        if (options.Mode == AuthMode.Oidc && string.IsNullOrWhiteSpace(options.Authority))
        {
            throw new InvalidOperationException("Auth:Authority is required when Auth:Mode is 'Oidc'.");
        }

        if (options.Mode == AuthMode.GuestTokens && !environment.IsDevelopment() && !options.AllowGuestTokensOutsideDevelopment)
        {
            throw new InvalidOperationException(
                "Guest tokens are disabled outside Development. Configure Auth:Mode=Oidc, " +
                "or set Auth:AllowGuestTokensOutsideDevelopment=true for a demo deployment.");
        }

        if (options.Mode == AuthMode.GuestTokens && !environment.IsDevelopment() && string.IsNullOrWhiteSpace(options.GuestSigningKey))
        {
            throw new InvalidOperationException(
                "Auth:GuestSigningKey is required outside Development. Supply a Base64 key of at least 256 bits " +
                "through a secret store or the Auth__GuestSigningKey environment variable.");
        }
    }
}
