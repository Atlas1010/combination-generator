using System.ComponentModel.DataAnnotations;

namespace CombinationGenerator.Infrastructure.Sessions;

public enum SessionStoreProvider
{
    Memory,
    Redis,
}

public sealed class SessionStoreOptions
{
    public const string SectionName = "SessionStore";

    public SessionStoreProvider Provider { get; init; } = SessionStoreProvider.Memory;

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(30);

    [Range(1, 1_000_000)]
    public int MaxSessions { get; init; } = 10_000;
}
