using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Combinations;

public sealed record StartResult(Guid SessionId, int Size, long TotalCount);

public sealed record NextResult(SequenceItem? Item, bool HasMore, long TotalCount, string? Message);

public sealed record PageQuery(int PageSize, long PageNumber, long? Anchor);

public sealed record PageResult(SequencePage Page, long TotalCount, string? Message);

public static class UserMessages
{
    public const string NoMoreCombinations = "אין יותר קומבינציות";
}
