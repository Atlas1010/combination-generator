using CombinationGenerator.Application.Combinations;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Api.Contracts;

// Maps application results to the public HTTP contract.
internal static class ContractMapping
{
    public static StartResponse ToResponse(this StartResult result) =>
        new(result.SessionId, result.Size, result.TotalCount);

    public static NextResponse ToResponse(this NextResult result) =>
        new(result.Item?.ToDto(), result.HasMore, result.TotalCount, result.Message);

    public static PageResponse ToResponse(this PageResult result) =>
        new(
            result.Page.Items.Select(ToDto).ToArray(),
            result.Page.Anchor,
            result.Page.PageNumber,
            result.Page.PageSize,
            result.Page.TotalPages,
            result.Page.TotalItems,
            result.TotalCount,
            result.Message);

    public static PageQuery ToQuery(this GetAllRequest request) =>
        new(request.PageSize, request.PageNumber, request.Anchor);

    private static CombinationDto ToDto(this SequenceItem item) => new(item.Ordinal, item.Values);
}
