namespace CombinationGenerator.Application.Combinations;

// Combination generator use cases, scoped to the current user.
public interface ICombinationGeneratorService
{
    // Opens a new session for n numbers.
    Task<StartResult> StartAsync(int size, CancellationToken cancellationToken);

    // Returns the arrangement after the last one the user has seen.
    Task<NextResult> GetNextAsync(Guid sessionId, CancellationToken cancellationToken);

    // Returns one page of the arrangements that follow the anchor.
    Task<PageResult> GetPageAsync(Guid sessionId, PageQuery query, CancellationToken cancellationToken);
}
