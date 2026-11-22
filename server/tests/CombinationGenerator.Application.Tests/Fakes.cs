using System.Collections.Concurrent;
using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Tests;

internal sealed class FakeSessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, SessionSnapshot> _sessions = new();

    public Task<SessionSnapshot?> FindAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var snapshot) ? snapshot : null);

    public int ConflictsToSimulate { get; set; }

    public Task AddAsync(SessionSnapshot session, CancellationToken cancellationToken)
    {
        _sessions[session.Id] = Copy(session);
        return Task.CompletedTask;
    }

    public Task<bool> TryUpdateAsync(SessionSnapshot session, CancellationToken cancellationToken)
    {
        if (ConflictsToSimulate > 0)
        {
            ConflictsToSimulate--;
            return Task.FromResult(false);
        }

        var expected = _sessions.TryGetValue(session.Id, out var stored) ? stored : null;
        var updated = expected is not null
            && expected.Version == session.Version - 1
            && _sessions.TryUpdate(session.Id, Copy(session), expected);
        return Task.FromResult(updated);
    }

    private static SessionSnapshot Copy(SessionSnapshot session) => session with { Current = session.Current?.ToArray() };
}

internal sealed class FakeCurrentUser(string id) : ICurrentUser
{
    public string Id { get; set; } = id;
}

internal sealed class FakeSessionLock : ISessionLock
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async ValueTask<IAsyncDisposable> AcquireAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Releaser(_gate);
    }

    private sealed class Releaser(SemaphoreSlim gate) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            gate.Release();
            return ValueTask.CompletedTask;
        }
    }
}
