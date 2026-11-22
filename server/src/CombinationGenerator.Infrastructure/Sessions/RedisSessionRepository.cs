using System.Text.Json;
using System.Text.Json.Serialization;
using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Domain.Sessions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CombinationGenerator.Infrastructure.Sessions;

// Stores sessions in Redis using atomic Lua scripts.
internal sealed class RedisSessionRepository(IConnectionMultiplexer redis, IOptions<SessionStoreOptions> options)
    : ISessionRepository
{
    private const string KeyPrefix = "combination-generator:session:";

    private const string FindScript = """
        local d = redis.call('HGET', KEYS[1], 'd')
        if d then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
        return d
        """;

    private const string AddScript = """
        redis.call('HSET', KEYS[1], 'v', ARGV[1], 'd', ARGV[2])
        redis.call('PEXPIRE', KEYS[1], ARGV[3])
        return 1
        """;

    private const string UpdateScript = """
        if redis.call('HGET', KEYS[1], 'v') ~= ARGV[1] then return 0 end
        redis.call('HSET', KEYS[1], 'v', ARGV[2], 'd', ARGV[3])
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
        return 1
        """;

    private IDatabase Database => redis.GetDatabase();

    private long IdleTimeoutMs => (long)options.Value.IdleTimeout.TotalMilliseconds;

    public async Task<SessionSnapshot?> FindAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await Database.ScriptEvaluateAsync(FindScript, [Key(sessionId)], [IdleTimeoutMs]);
        return result.IsNull
            ? null
            : JsonSerializer.Deserialize((byte[])result!, SessionJsonContext.Default.SessionSnapshot);
    }

    public Task AddAsync(SessionSnapshot session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Database.ScriptEvaluateAsync(AddScript, [Key(session.Id)], [session.Version, Serialize(session), IdleTimeoutMs]);
    }

    public async Task<bool> TryUpdateAsync(SessionSnapshot session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        var result = await Database.ScriptEvaluateAsync(
            UpdateScript,
            [Key(session.Id)],
            [session.Version - 1, session.Version, Serialize(session), IdleTimeoutMs]);
        return (long)result == 1;
    }

    private static RedisKey Key(Guid sessionId) => KeyPrefix + sessionId.ToString("N");

    private static byte[] Serialize(SessionSnapshot session) =>
        JsonSerializer.SerializeToUtf8Bytes(session, SessionJsonContext.Default.SessionSnapshot);
}

// Source-generated JSON serializer for session snapshots.
[JsonSerializable(typeof(SessionSnapshot))]
internal sealed partial class SessionJsonContext : JsonSerializerContext;
