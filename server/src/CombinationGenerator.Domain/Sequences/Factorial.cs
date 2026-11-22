namespace CombinationGenerator.Domain.Sequences;

// Precomputed factorials from 0! to 20!.
public static class Factorial
{
    public const int MaxInput = 20;

    private static readonly long[] Table = Build();

    public static long Of(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n, MaxInput);
        return Table[n];
    }

    private static long[] Build()
    {
        var table = new long[MaxInput + 1];
        table[0] = 1;
        for (var i = 1; i <= MaxInput; i++)
        {
            table[i] = checked(table[i - 1] * i);
        }

        return table;
    }
}
