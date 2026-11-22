using CombinationGenerator.Domain.Sequences;

namespace CombinationGenerator.Domain.Sessions;

// Aggregate root tracking a user's walk through a combinatorial sequence.
public sealed class GenerationSession
{
    private readonly ICombinatorialSequence _sequence;
    private int[]? _current;

    private GenerationSession(Guid id, string ownerId, ICombinatorialSequence sequence, long position, int[]? current, long version)
    {
        Id = id;
        OwnerId = ownerId;
        _sequence = sequence;
        Position = position;
        _current = current;
        Version = version;
    }

    public Guid Id { get; }

    public long Version { get; }

    public string OwnerId { get; }

    public int Size => _sequence.Size;

    public long TotalCount => _sequence.Count;

    public long Position { get; private set; }

    public bool HasMore => Position < TotalCount;

    public static GenerationSession Start(string ownerId, ICombinatorialSequence sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(sequence);
        return new GenerationSession(Guid.NewGuid(), ownerId, sequence, position: 0, current: null, version: 0);
    }

    public static GenerationSession Restore(SessionSnapshot snapshot, ICombinatorialSequence sequence)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(sequence);

        if (snapshot.Position < 0 || snapshot.Position > sequence.Count)
        {
            throw new InvalidOperationException($"Session {snapshot.Id} has a corrupt position.");
        }

        var current = snapshot.Position > 0 && snapshot.Current is { } cached && cached.Length == sequence.Size
            ? cached.ToArray()
            : null;

        return new GenerationSession(snapshot.Id, snapshot.OwnerId, sequence, snapshot.Position, current, snapshot.Version);
    }

    // Returns the state to persist, with the next version.
    public SessionSnapshot ToSnapshot() => new(Id, OwnerId, Size, Position, _current?.ToArray(), Version + 1);

    public bool IsOwnedBy(string userId) => string.Equals(OwnerId, userId, StringComparison.Ordinal);

    // Advances to the next arrangement, or returns null when exhausted.
    public SequenceItem? MoveNext()
    {
        if (!HasMore)
        {
            return null;
        }

        if (_current is null)
        {
            _current = _sequence.ElementAt(Position);
        }
        else if (!_sequence.TryMoveNext(_current))
        {
            throw new InvalidOperationException($"Session {Id} cursor is inconsistent with its arrangement.");
        }

        Position++;
        return new SequenceItem(Position, _current.ToArray());
    }

    // Reads a page after the anchor and moves the cursor to its end.
    public SequencePage ReadPage(int pageSize, long pageNumber, long? anchor)
    {
        if (pageSize < 1)
        {
            throw new DomainValidationException("Page size must be at least 1.");
        }

        if (pageNumber < 1)
        {
            throw new DomainValidationException("Page number must be at least 1.");
        }

        var start = anchor ?? Position;
        if (start < 0 || start > TotalCount)
        {
            throw new DomainValidationException($"Anchor must be between 0 and {TotalCount}.");
        }

        var totalItems = TotalCount - start;
        var totalPages = (totalItems / pageSize) + (totalItems % pageSize == 0 ? 0 : 1);
        if (totalPages == 0)
        {
            return new SequencePage(start, pageNumber, pageSize, TotalPages: 0, TotalItems: 0, Items: []);
        }

        if (pageNumber > totalPages)
        {
            throw new DomainValidationException($"Page number must be between 1 and {totalPages}.");
        }

        var firstRank = start + ((pageNumber - 1) * pageSize);
        var count = (int)Math.Min(pageSize, TotalCount - firstRank);

        var buffer = _sequence.ElementAt(firstRank);
        var items = new SequenceItem[count];
        items[0] = new SequenceItem(firstRank + 1, buffer.ToArray());
        for (var i = 1; i < count; i++)
        {
            _sequence.TryMoveNext(buffer);
            items[i] = new SequenceItem(firstRank + i + 1, buffer.ToArray());
        }

        Position = firstRank + count;
        _current = buffer;

        return new SequencePage(start, pageNumber, pageSize, totalPages, totalItems, items);
    }
}
