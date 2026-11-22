using CombinationGenerator.Application.Combinations;
using CombinationGenerator.Domain.Sequences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CombinationGenerator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GeneratorOptions>()
            .Bind(configuration.GetSection(GeneratorOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISequenceFactory, PermutationSequenceFactory>();
        services.AddScoped<ICombinationGeneratorService, CombinationGeneratorService>();
        return services;
    }
}
