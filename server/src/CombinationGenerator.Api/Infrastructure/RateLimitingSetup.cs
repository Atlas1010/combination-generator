using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CombinationGenerator.Api.Infrastructure;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 10_000)]
    public int RequestsPerSecond { get; init; } = 20;

    [Range(1, 10_000)]
    public int BurstLimit { get; init; } = 40;

    [Range(1, 10_000)]
    public int GuestTokensPerMinute { get; init; } = 20;
}

internal static class RateLimitingSetup
{
    public const string ApiPolicy = "api";
    public const string TokenIssuancePolicy = "token-issuance";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RateLimitingOptions.SectionName);
        services.AddOptions<RateLimitingOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();
        var options = section.Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new()
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = { Status = StatusCodes.Status429TooManyRequests, Title = "Too many requests." },
                });
            };

            limiter.AddPolicy(ApiPolicy, http => RateLimitPartition.GetTokenBucketLimiter(
                http.User.Identity?.Name ?? ClientAddress(http),
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = options.BurstLimit,
                    TokensPerPeriod = options.RequestsPerSecond,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));

            limiter.AddPolicy(TokenIssuancePolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                ClientAddress(http),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.GuestTokensPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return services;
    }

    private static string ClientAddress(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
