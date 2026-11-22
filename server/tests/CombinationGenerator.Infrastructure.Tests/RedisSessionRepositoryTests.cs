using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Infrastructure.Sessions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CombinationGenerator.Infrastructure.Tests;

// Runs the shared repository contract against Redis.
public sealed class RedisSessionRepositoryTests : SessionRepositoryContract, IDisposable
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION");

    private ConnectionMultiplexer? _connection;

    protected override ISessionRepository CreateRepository() => new RedisSessionRepository(
        Connect(), Options.Create(new SessionStoreOptions { IdleTimeout = TimeSpan.FromMinutes(5) }));

    [Fact]
    public async Task Every_read_slides_the_idle_expiration()
    {
        var connection = Connect();
        var repository = CreateRepository();
        var session = NewSession();
        await repository.AddAsync(session, Ct);
        var key = "combination-generator:session:" + session.Id.ToString("N");

        await connection.GetDatabase().KeyExpireAsync(key, TimeSpan.FromSeconds(10));
        await repository.FindAsync(session.Id, Ct);

        var ttl = await connection.GetDatabase().KeyTimeToLiveAsync(key);
        Assert.InRange(ttl!.Value, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5));
    }

    public void Dispose() => _connection?.Dispose();

    private ConnectionMultiplexer Connect()
    {
        Assert.SkipWhen(string.IsNullOrWhiteSpace(ConnectionString), "REDIS_TEST_CONNECTION is not set.");
        return _connection ??= ConnectionMultiplexer.Connect(ConnectionString!);
    }
}
