namespace CombinationGenerator.Domain.Sequences;

// All permutations of 1..n in lexicographic order.
public sealed class PermutationSequence : ICombinatorialSequence
{
    public const int MinSize = 1;
    public const int MaxSize = Factorial.MaxInput;

    public PermutationSequence(int size)
    {
        if (size is < MinSize or > MaxSize)
        {
            throw new DomainValidationException($"n must be an integer between {MinSize} and {MaxSize}.");
        }

        Size = size;
        Count = Factorial.Of(size);
    }

    public int Size { get; }

    public long Count { get; }

    public int[] ElementAt(long rank)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rank);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rank, Count);

        Span<int> available = stackalloc int[Size];
        for (var i = 0; i < Size; i++)
        {
            available[i] = i + 1;
        }

        var result = new int[Size];
        var remaining = Size;
        for (var position = 0; position < Size; position++)
        {
            var blockSize = Factorial.Of(remaining - 1);
            var choice = (int)(rank / blockSize);
            rank %= blockSize;

            result[position] = available[choice];
            available[(choice + 1)..remaining].CopyTo(available[choice..]);
            remaining--;
        }

        return result;
    }

    public bool TryMoveNext(Span<int> current)
    {
        if (current.Length != Size)
        {
            throw new ArgumentException($"Expected a permutation of length {Size}.", nameof(current));
        }

        var pivot = current.Length - 2;
        while (pivot >= 0 && current[pivot] >= current[pivot + 1])
        {
            pivot--;
        }

        if (pivot < 0)
        {
            return false;
        }

        var successor = current.Length - 1;
        while (current[successor] <= current[pivot])
        {
            successor--;
        }

        (current[pivot], current[successor]) = (current[successor], current[pivot]);

        current[(pivot + 1)..].Reverse();
        return true;
    }

    // Returns the zero-based rank of a permutation.
    public long RankOf(ReadOnlySpan<int> permutation)
    {
        if (permutation.Length != Size)
        {
            throw new ArgumentException($"Expected a permutation of length {Size}.", nameof(permutation));
        }

        long rank = 0;
        for (var i = 0; i < Size; i++)
        {
            var smallerToTheRight = 0;
            for (var j = i + 1; j < Size; j++)
            {
                if (permutation[j] < permutation[i])
                {
                    smallerToTheRight++;
                }
            }

            rank += smallerToTheRight * Factorial.Of(Size - 1 - i);
        }

        return rank;
    }
}
