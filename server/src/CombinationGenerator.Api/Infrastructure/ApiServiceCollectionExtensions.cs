using System.Text.Json.Serialization;
using CombinationGenerator.Api.Security;
using Microsoft.OpenApi;

namespace CombinationGenerator.Api.Infrastructure;

internal static class ApiServiceCollectionExtensions
{
    public const string CorsPolicy = "client";

    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers()
            .AddJsonOptions(json => json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(problem => problem.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiAuthentication(configuration, environment);
        services.AddApiRateLimiting(configuration);
        services.AddHealthChecks();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(cors => cors.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST")
            .WithHeaders("Authorization", "Content-Type")));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(swagger =>
        {
            swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "Combination Generator API", Version = "v1" });
            swagger.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "CombinationGenerator.Api.xml"));
            swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Call POST /api/auth/guest-token and paste the accessToken here.",
            });
            swagger.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
        });

        return services;
    }
}
