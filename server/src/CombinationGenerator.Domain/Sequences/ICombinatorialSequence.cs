namespace CombinationGenerator.Domain.Sequences;

// A finite ordered sequence of arrangements of the numbers 1..n.
public interface ICombinatorialSequence
{
    int Size { get; }

    long Count { get; }

    // Builds the arrangement at the given zero-based rank.
    int[] ElementAt(long rank);

    // Advances the arrangement in place to its successor.
    bool TryMoveNext(Span<int> current);
}
