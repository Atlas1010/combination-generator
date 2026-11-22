using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Application.Common;
using CombinationGenerator.Infrastructure.Sessions;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Infrastructure.Tests;

public sealed class InMemorySessionRepositoryTests : SessionRepositoryContract, IDisposable
{
    private readonly List<InMemorySessionRepository> _created = [];

    protected override ISessionRepository CreateRepository() => Create(maxSessions: 100);

    [Fact]
    public async Task A_full_store_refuses_new_sessions()
    {
        var repository = Create(maxSessions: 1);
        await repository.AddAsync(NewSession(), Ct);

        await Assert.ThrowsAsync<SessionCapacityExceededException>(() => repository.AddAsync(NewSession(), Ct));
    }

    public void Dispose()
    {
        foreach (var repository in _created)
        {
            repository.Dispose();
        }
    }

    private InMemorySessionRepository Create(int maxSessions)
    {
        var repository = new InMemorySessionRepository(Options.Create(new SessionStoreOptions { MaxSessions = maxSessions }));
        _created.Add(repository);
        return repository;
    }
}
