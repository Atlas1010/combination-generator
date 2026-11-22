using CombinationGenerator.Domain.Sequences;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Domain.Tests;

public sealed class GenerationSessionTests
{
    private const string Owner = "user-1";

    [Fact]
    public void MoveNext_returns_combinations_in_order_with_their_ordinals()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(3));

        var first = session.MoveNext();
        var second = session.MoveNext();

        Assert.Equal(1, first!.Ordinal);
        Assert.Equal([1, 2, 3], first.Values);
        Assert.Equal(2, second!.Ordinal);
        Assert.Equal([1, 3, 2], second.Values);
    }

    [Fact]
    public void MoveNext_returns_null_once_exhausted()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(2));

        Assert.NotNull(session.MoveNext());
        Assert.NotNull(session.MoveNext());
        Assert.False(session.HasMore);
        Assert.Null(session.MoveNext());
        Assert.Equal(2, session.Position);
    }

    [Fact]
    public void Page_starts_right_after_the_current_position()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(5));
        for (var i = 0; i < 10; i++)
        {
            session.MoveNext();
        }

        var page = session.ReadPage(pageSize: 7, pageNumber: 1, anchor: null);

        Assert.Equal(10, page.Anchor);
        Assert.Equal(110, page.TotalItems);
        Assert.Equal(16, page.TotalPages);
        Assert.Equal(Enumerable.Range(11, 7).Select(i => (long)i), page.Items.Select(item => item.Ordinal));
    }

    [Fact]
    public void Pages_are_counted_from_the_anchor_and_the_last_page_may_be_partial()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(4));
        var sequence = new PermutationSequence(4);

        var last = session.ReadPage(pageSize: 10, pageNumber: 3, anchor: 0);

        Assert.Equal(3, last.TotalPages);
        Assert.Equal([21L, 22L, 23L, 24L], last.Items.Select(item => item.Ordinal));
        Assert.All(last.Items, item => Assert.Equal(sequence.ElementAt(item.Ordinal - 1), item.Values));
    }

    [Fact]
    public void Reading_a_page_moves_the_cursor_to_its_last_item_so_next_continues_from_there()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(5));
        session.ReadPage(pageSize: 20, pageNumber: 3, anchor: 0);

        Assert.Equal(60, session.Position);
        Assert.Equal(61, session.MoveNext()!.Ordinal);
    }

    [Fact]
    public void Page_beyond_the_last_page_is_rejected()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(3));

        Assert.Throws<DomainValidationException>(() => session.ReadPage(pageSize: 2, pageNumber: 4, anchor: 0));
    }

    [Fact]
    public void Paging_when_nothing_remains_returns_an_empty_page()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(2));
        session.MoveNext();
        session.MoveNext();

        var page = session.ReadPage(pageSize: 5, pageNumber: 1, anchor: null);

        Assert.True(page.IsEmpty);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public void The_last_page_of_n_20_is_reachable_without_iterating()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(20));
        const int pageSize = 500;
        var totalPages = (session.TotalCount + pageSize - 1) / pageSize;

        var page = session.ReadPage(pageSize, totalPages, anchor: 0);

        Assert.Equal(session.TotalCount, page.Items[^1].Ordinal);
        Assert.Equal(Enumerable.Range(1, 20).Reverse(), page.Items[^1].Values);
        Assert.False(session.HasMore);
    }

    [Fact]
    public void Snapshot_round_trip_preserves_the_cursor()
    {
        var session = GenerationSession.Start(Owner, new PermutationSequence(6));
        for (var i = 0; i < 100; i++)
        {
            session.MoveNext();
        }

        var restored = GenerationSession.Restore(session.ToSnapshot(), new PermutationSequence(6));

        Assert.Equal(session.MoveNext(), restored.MoveNext(), SequenceItemComparer.Instance);
    }

    [Fact]
    public void Restoring_a_snapshot_without_a_cached_arrangement_rebuilds_it_from_the_position()
    {
        var snapshot = new SessionSnapshot(Guid.NewGuid(), Owner, Size: 4, Position: 5, Current: null, Version: 1);

        var restored = GenerationSession.Restore(snapshot, new PermutationSequence(4));

        var next = restored.MoveNext()!;
        Assert.Equal(6, next.Ordinal);
        Assert.Equal(new PermutationSequence(4).ElementAt(5), next.Values);
    }

    private sealed class SequenceItemComparer : IEqualityComparer<SequenceItem?>
    {
        public static readonly SequenceItemComparer Instance = new();

        public bool Equals(SequenceItem? x, SequenceItem? y) =>
            x?.Ordinal == y?.Ordinal && (x?.Values ?? []).SequenceEqual(y?.Values ?? []);

        public int GetHashCode(SequenceItem? obj) => obj?.Ordinal.GetHashCode() ?? 0;
    }
}
