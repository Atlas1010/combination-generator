using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Application.Common;
using CombinationGenerator.Domain.Sessions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Infrastructure.Sessions;

// Stores sessions in a size-bounded memory cache with sliding expiration.
internal sealed class InMemorySessionRepository : ISessionRepository, IDisposable
{
    private readonly MemoryCache _cache;
    private readonly SessionStoreOptions _options;
    private readonly object _gate = new();

    public InMemorySessionRepository(IOptions<SessionStoreOptions> options)
    {
        _options = options.Value;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = _options.MaxSessions });
    }

    public Task<SessionSnapshot?> FindAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(_cache.TryGetValue(sessionId, out SessionSnapshot? snapshot) ? snapshot : null);

    public Task AddAsync(SessionSnapshot session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (_gate)
        {
            Store(session);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryUpdateAsync(SessionSnapshot session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (_gate)
        {
            if (!_cache.TryGetValue(session.Id, out SessionSnapshot? stored) || stored!.Version != session.Version - 1)
            {
                return Task.FromResult(false);
            }

            Store(session);
        }

        return Task.FromResult(true);
    }

    public void Dispose() => _cache.Dispose();

    private void Store(SessionSnapshot session)
    {
        var stored = session with { Current = session.Current?.ToArray() };
        _cache.Set(session.Id, stored, new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = _options.IdleTimeout,
        });

        if (!_cache.TryGetValue(session.Id, out _))
        {
            throw new SessionCapacityExceededException();
        }
    }
}
