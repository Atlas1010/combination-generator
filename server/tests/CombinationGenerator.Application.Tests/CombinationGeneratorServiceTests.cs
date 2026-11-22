using CombinationGenerator.Application.Combinations;
using CombinationGenerator.Application.Common;
using CombinationGenerator.Domain;
using CombinationGenerator.Domain.Sequences;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Application.Tests;

public sealed class CombinationGeneratorServiceTests
{
    private readonly FakeSessionRepository _repository = new();
    private readonly FakeCurrentUser _user = new("alice");
    private readonly CombinationGeneratorService _service;

    public CombinationGeneratorServiceTests() => _service = CreateService();

    // Creates a service with its own lock, like a separate instance.
    private CombinationGeneratorService CreateService() =>
        new(
            _repository,
            new FakeSessionLock(),
            new PermutationSequenceFactory(),
            _user,
            Options.Create(new GeneratorOptions { MaxPageSize = 100 }),
            NullLogger<CombinationGeneratorService>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Start_returns_the_total_number_of_combinations()
    {
        var result = await _service.StartAsync(20, Ct);

        Assert.Equal(20, result.Size);
        Assert.Equal(2_432_902_008_176_640_000L, result.TotalCount);
    }

    [Fact]
    public async Task Next_walks_the_sequence_and_then_reports_that_nothing_is_left()
    {
        var session = await _service.StartAsync(2, Ct);

        var first = await _service.GetNextAsync(session.SessionId, Ct);
        var second = await _service.GetNextAsync(session.SessionId, Ct);
        var none = await _service.GetNextAsync(session.SessionId, Ct);

        Assert.Equal([1, 2], first.Item!.Values);
        Assert.True(first.HasMore);
        Assert.Equal([2, 1], second.Item!.Values);
        Assert.False(second.HasMore);
        Assert.Null(none.Item);
        Assert.Equal(UserMessages.NoMoreCombinations, none.Message);
    }

    [Fact]
    public async Task Show_all_continues_after_the_combination_currently_on_screen()
    {
        var session = await _service.StartAsync(7, Ct);
        for (var i = 0; i < 1000; i++)
        {
            await _service.GetNextAsync(session.SessionId, Ct);
        }

        var page = await _service.GetPageAsync(session.SessionId, new PageQuery(50, 1, Anchor: null), Ct);

        Assert.Equal(1000, page.Page.Anchor);
        Assert.Equal(1001, page.Page.Items[0].Ordinal);
    }

    [Fact]
    public async Task Back_from_show_all_continues_after_the_last_combination_shown()
    {
        var session = await _service.StartAsync(5, Ct);
        var first = await _service.GetPageAsync(session.SessionId, new PageQuery(10, 1, Anchor: null), Ct);
        await _service.GetPageAsync(session.SessionId, new PageQuery(10, 4, first.Page.Anchor), Ct);

        var next = await _service.GetNextAsync(session.SessionId, Ct);

        Assert.Equal(41, next.Item!.Ordinal);
    }

    [Fact]
    public async Task A_session_cannot_be_used_by_another_user()
    {
        var session = await _service.StartAsync(3, Ct);

        _user.Id = "mallory";

        await Assert.ThrowsAsync<SessionNotFoundException>(() => _service.GetNextAsync(session.SessionId, Ct));
    }

    [Fact]
    public async Task Unknown_session_is_reported_as_not_found() =>
        await Assert.ThrowsAsync<SessionNotFoundException>(() => _service.GetNextAsync(Guid.NewGuid(), Ct));

    [Fact]
    public async Task Page_size_above_the_configured_maximum_is_rejected()
    {
        var session = await _service.StartAsync(5, Ct);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.GetPageAsync(session.SessionId, new PageQuery(101, 1, null), Ct));
    }

    [Fact]
    public async Task Concurrent_next_calls_on_one_session_never_return_the_same_combination_twice()
    {
        var session = await _service.StartAsync(6, Ct);

        var results = await Task.WhenAll(Enumerable.Range(0, 200)
            .Select(_ => Task.Run(() => _service.GetNextAsync(session.SessionId, Ct), Ct)));

        var ordinals = results.Select(r => r.Item!.Ordinal).Order().ToArray();
        Assert.Equal(Enumerable.Range(1, 200).Select(i => (long)i), ordinals);
    }

    [Fact]
    public async Task Two_api_instances_sharing_a_store_never_return_the_same_combination_twice()
    {
        var otherInstance = CreateService();
        var session = await _service.StartAsync(6, Ct);

        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => (i % 2 == 0 ? _service : otherInstance).GetNextAsync(session.SessionId, Ct), Ct)));

        var ordinals = results.Select(r => r.Item!.Ordinal).Order().ToArray();
        Assert.Equal(Enumerable.Range(1, 100).Select(i => (long)i), ordinals);
    }

    [Fact]
    public async Task A_concurrent_write_is_retried_on_fresh_state()
    {
        var session = await _service.StartAsync(3, Ct);
        _repository.ConflictsToSimulate = 2;

        var first = await _service.GetNextAsync(session.SessionId, Ct);

        Assert.Equal(1, first.Item!.Ordinal);
    }

    [Fact]
    public async Task A_session_that_keeps_changing_is_reported_as_a_conflict()
    {
        var session = await _service.StartAsync(3, Ct);
        _repository.ConflictsToSimulate = int.MaxValue;

        await Assert.ThrowsAsync<SessionConflictException>(() => _service.GetNextAsync(session.SessionId, Ct));
    }

    [Fact]
    public async Task Sessions_of_different_users_are_independent()
    {
        var aliceSession = await _service.StartAsync(4, Ct);
        await _service.GetNextAsync(aliceSession.SessionId, Ct);
        await _service.GetNextAsync(aliceSession.SessionId, Ct);

        _user.Id = "bob";
        var bobSession = await _service.StartAsync(4, Ct);
        var bobFirst = await _service.GetNextAsync(bobSession.SessionId, Ct);

        _user.Id = "alice";
        var aliceThird = await _service.GetNextAsync(aliceSession.SessionId, Ct);

        Assert.Equal(1, bobFirst.Item!.Ordinal);
        Assert.Equal(3, aliceThird.Item!.Ordinal);
    }
}
