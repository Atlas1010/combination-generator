using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Infrastructure.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CombinationGenerator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SessionStoreOptions.SectionName);
        services.AddOptions<SessionStoreOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISessionLock, StripedSessionLock>();

        var provider = section.GetValue(nameof(SessionStoreOptions.Provider), SessionStoreProvider.Memory);
        switch (provider)
        {
            case SessionStoreProvider.Redis:
                var connectionString = configuration.GetConnectionString("Redis");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "SessionStore:Provider is 'Redis' but ConnectionStrings:Redis is not configured.");
                }

                services.AddSingleton<IConnectionMultiplexer>(_ =>
                {
                    var redisOptions = ConfigurationOptions.Parse(connectionString);
                    redisOptions.AbortOnConnectFail = false;
                    return ConnectionMultiplexer.Connect(redisOptions);
                });
                services.AddSingleton<ISessionRepository, RedisSessionRepository>();
                break;

            default:
                services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
                break;
        }

        return services;
    }
}
