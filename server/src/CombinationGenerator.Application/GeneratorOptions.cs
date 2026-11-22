using System.ComponentModel.DataAnnotations;

namespace CombinationGenerator.Application;

public sealed class GeneratorOptions
{
    public const string SectionName = "Generator";

    [Range(1, 10_000)]
    public int MaxPageSize { get; init; } = 500;
}
