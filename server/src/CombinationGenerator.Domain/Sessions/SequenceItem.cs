namespace CombinationGenerator.Domain.Sessions;

// An arrangement together with its 1-based ordinal.
public sealed record SequenceItem(long Ordinal, IReadOnlyList<int> Values);

// One page of arrangements counted from the anchor.
public sealed record SequencePage(
    long Anchor,
    long PageNumber,
    int PageSize,
    long TotalPages,
    long TotalItems,
    IReadOnlyList<SequenceItem> Items)
{
    public bool IsEmpty => Items.Count == 0;
}
