using CombinationGenerator.Domain.Sequences;

namespace CombinationGenerator.Domain.Tests;

public sealed class PermutationSequenceTests
{
    [Theory]
    [InlineData(1, 1L)]
    [InlineData(3, 6L)]
    [InlineData(10, 3_628_800L)]
    [InlineData(20, 2_432_902_008_176_640_000L)]
    public void Count_is_n_factorial(int n, long expected) =>
        Assert.Equal(expected, new PermutationSequence(n).Count);

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public void Size_outside_1_to_20_is_rejected(int n) =>
        Assert.Throws<DomainValidationException>(() => new PermutationSequence(n));

    [Fact]
    public void Successor_walk_for_n_3_matches_the_specification_example()
    {
        var sequence = new PermutationSequence(3);
        var current = sequence.ElementAt(0);
        var all = new List<string> { Format(current) };
        while (sequence.TryMoveNext(current))
        {
            all.Add(Format(current));
        }

        Assert.Equal(["[1,2,3]", "[1,3,2]", "[2,1,3]", "[2,3,1]", "[3,1,2]", "[3,2,1]"], all);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(7)]
    public void Unranking_agrees_with_the_successor_walk_for_every_rank(int n)
    {
        var sequence = new PermutationSequence(n);
        var walked = sequence.ElementAt(0);
        for (long rank = 0; rank < sequence.Count; rank++)
        {
            Assert.Equal(walked, sequence.ElementAt(rank));
            Assert.Equal(rank, sequence.RankOf(walked));
            Assert.Equal(rank < sequence.Count - 1, sequence.TryMoveNext(walked));
        }
    }

    [Fact]
    public void Successor_of_the_last_permutation_fails_and_leaves_it_unchanged()
    {
        var sequence = new PermutationSequence(4);
        int[] last = [4, 3, 2, 1];

        Assert.False(sequence.TryMoveNext(last));
        Assert.Equal([4, 3, 2, 1], last);
    }

    [Fact]
    public void Random_access_works_at_the_extremes_of_n_20()
    {
        var sequence = new PermutationSequence(20);

        Assert.Equal(Enumerable.Range(1, 20), sequence.ElementAt(0));
        Assert.Equal(Enumerable.Range(1, 20).Reverse(), sequence.ElementAt(sequence.Count - 1));

        var middle = sequence.Count / 2;
        Assert.Equal(middle, sequence.RankOf(sequence.ElementAt(middle)));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(6L)]
    public void Rank_outside_the_sequence_is_rejected(long rank) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PermutationSequence(3).ElementAt(rank));

    private static string Format(int[] values) => $"[{string.Join(',', values)}]";
}
