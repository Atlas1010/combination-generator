using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CombinationGenerator.Domain.Sequences;

namespace CombinationGenerator.Api.Contracts;

// StartAPI request.
public sealed record StartRequest(
    [Range(PermutationSequence.MinSize, PermutationSequence.MaxSize, ErrorMessage = "n must be an integer between {1} and {2}.")]
    int N);

// StartAPI response.
public sealed record StartResponse(
    Guid SessionId,
    int N,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long TotalCount);

// A single arrangement and its 1-based ordinal.
public sealed record CombinationDto(
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long Ordinal,
    IReadOnlyList<int> Values);

// GetNextAPI response.
public sealed record NextResponse(
    CombinationDto? Combination,
    bool HasMore,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long TotalCount,
    string? Message);

// GetAllAPI request.
public sealed record GetAllRequest(
    [Range(1, int.MaxValue)] int PageSize,
    [Range(typeof(long), "1", "9223372036854775807")]
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    long PageNumber = 1,
    [Range(typeof(long), "0", "9223372036854775807")]
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    long? Anchor = null);

// GetAllAPI response: one page of combinations plus paging metadata.
public sealed record PageResponse(
    IReadOnlyList<CombinationDto> Items,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long Anchor,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long PageNumber,
    int PageSize,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long TotalPages,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long TotalItems,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long TotalCount,
    string? Message);
