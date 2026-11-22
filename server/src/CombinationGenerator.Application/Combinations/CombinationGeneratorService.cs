using CombinationGenerator.Application.Abstractions;
using CombinationGenerator.Application.Common;
using CombinationGenerator.Domain;
using CombinationGenerator.Domain.Sequences;
using CombinationGenerator.Domain.Sessions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CombinationGenerator.Application.Combinations;

internal sealed partial class CombinationGeneratorService(
    ISessionRepository repository,
    ISessionLock sessionLock,
    ISequenceFactory sequenceFactory,
    ICurrentUser currentUser,
    IOptions<GeneratorOptions> options,
    ILogger<CombinationGeneratorService> logger) : ICombinationGeneratorService
{
    private const int MaxSaveAttempts = 10;

    public async Task<StartResult> StartAsync(int size, CancellationToken cancellationToken)
    {
        var session = GenerationSession.Start(currentUser.Id, sequenceFactory.Create(size));
        await repository.AddAsync(session.ToSnapshot(), cancellationToken);

        LogSessionStarted(logger, session.Id, session.Size);
        return new StartResult(session.Id, session.Size, session.TotalCount);
    }

    public Task<NextResult> GetNextAsync(Guid sessionId, CancellationToken cancellationToken) =>
        UpdateSessionAsync(sessionId, session =>
        {
            var item = session.MoveNext();
            return new NextResult(item, session.HasMore, session.TotalCount, item is null ? UserMessages.NoMoreCombinations : null);
        }, cancellationToken);

    public Task<PageResult> GetPageAsync(Guid sessionId, PageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var maxPageSize = options.Value.MaxPageSize;
        if (query.PageSize > maxPageSize)
        {
            throw new DomainValidationException($"Page size must be between 1 and {maxPageSize}.");
        }

        return UpdateSessionAsync(sessionId, session =>
        {
            var page = session.ReadPage(query.PageSize, query.PageNumber, query.Anchor);
            return new PageResult(page, session.TotalCount, page.IsEmpty ? UserMessages.NoMoreCombinations : null);
        }, cancellationToken);
    }

    // Loads, authorizes, mutates and saves a session with retries.
    private async Task<TResult> UpdateSessionAsync<TResult>(
        Guid sessionId,
        Func<GenerationSession, TResult> operation,
        CancellationToken cancellationToken)
    {
        await using var _ = await sessionLock.AcquireAsync(sessionId, cancellationToken);

        for (var attempt = 1; attempt <= MaxSaveAttempts; attempt++)
        {
            var snapshot = await repository.FindAsync(sessionId, cancellationToken);
            if (snapshot is null)
            {
                throw new SessionNotFoundException(sessionId);
            }

            var session = GenerationSession.Restore(snapshot, sequenceFactory.Create(snapshot.Size));

            if (!session.IsOwnedBy(currentUser.Id))
            {
                LogForeignSessionAccess(logger, sessionId);
                throw new SessionNotFoundException(sessionId);
            }

            var result = operation(session);
            if (await repository.TryUpdateAsync(session.ToSnapshot(), cancellationToken))
            {
                return result;
            }

            LogSessionConflict(logger, sessionId, attempt);

            await Task.Delay(Random.Shared.Next(1, 5 * attempt), cancellationToken);
        }

        throw new SessionConflictException(sessionId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Session {SessionId} started with n={Size}")]
    private static partial void LogSessionStarted(ILogger logger, Guid sessionId, int size);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access to session {SessionId} denied: caller is not its owner")]
    private static partial void LogForeignSessionAccess(ILogger logger, Guid sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Session {SessionId} changed concurrently (attempt {Attempt}); retrying")]
    private static partial void LogSessionConflict(ILogger logger, Guid sessionId, int attempt);
}
