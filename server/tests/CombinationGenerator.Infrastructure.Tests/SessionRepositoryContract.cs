using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Infrastructure.Tests;

// Shared behaviour tests every session repository must pass.
public abstract class SessionRepositoryContract
{
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract ISessionRepository CreateRepository();

    protected static SessionSnapshot NewSession(long position = 0, int[]? current = null, long version = 1) =>
        new(Guid.NewGuid(), "guest:owner", Size: 3, position, current, version);

    [Fact]
    public async Task A_stored_session_can_be_found_again()
    {
        var repository = CreateRepository();
        var session = NewSession(position: 2, current: [1, 3, 2]);

        await repository.AddAsync(session, Ct);
        var found = await repository.FindAsync(session.Id, Ct);

        Assert.NotNull(found);
        Assert.Equal(session.OwnerId, found.OwnerId);
        Assert.Equal(2, found.Position);
        Assert.Equal([1, 3, 2], found.Current!);
        Assert.Equal(1, found.Version);
    }

    [Fact]
    public async Task An_unknown_session_is_not_found()
    {
        var repository = CreateRepository();

        Assert.Null(await repository.FindAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task An_update_based_on_the_stored_version_is_accepted()
    {
        var repository = CreateRepository();
        var session = NewSession();
        await repository.AddAsync(session, Ct);

        var updated = await repository.TryUpdateAsync(session with { Position = 1, Current = [1, 2, 3], Version = 2 }, Ct);

        Assert.True(updated);
        var found = await repository.FindAsync(session.Id, Ct);
        Assert.Equal(1, found!.Position);
        Assert.Equal(2, found.Version);
    }

    [Fact]
    public async Task An_update_based_on_a_stale_version_is_rejected()
    {
        var repository = CreateRepository();
        var session = NewSession();
        await repository.AddAsync(session, Ct);
        await repository.TryUpdateAsync(session with { Position = 1, Version = 2 }, Ct);

        var stale = await repository.TryUpdateAsync(session with { Position = 5, Version = 2 }, Ct);

        Assert.False(stale);
        Assert.Equal(1, (await repository.FindAsync(session.Id, Ct))!.Position);
    }

    [Fact]
    public async Task An_update_never_recreates_a_missing_session()
    {
        var repository = CreateRepository();
        var session = NewSession(version: 2);

        Assert.False(await repository.TryUpdateAsync(session, Ct));
        Assert.Null(await repository.FindAsync(session.Id, Ct));
    }

    [Fact]
    public async Task Of_many_simultaneous_writers_exactly_one_wins()
    {
        var repository = CreateRepository();
        var session = NewSession();
        await repository.AddAsync(session, Ct);

        var results = await Task.WhenAll(Enumerable.Range(1, 20).Select(position =>
            Task.Run(() => repository.TryUpdateAsync(session with { Position = position, Version = 2 }, Ct), Ct)));

        Assert.Single(results, won => won);
    }
}
