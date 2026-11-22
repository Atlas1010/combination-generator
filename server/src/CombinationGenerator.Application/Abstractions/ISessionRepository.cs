using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Abstractions;

// Persists sessions using optimistic concurrency.
public interface ISessionRepository
{
    // Loads a session and refreshes its idle expiration.
    Task<SessionSnapshot?> FindAsync(Guid sessionId, CancellationToken cancellationToken);

    // Stores a brand-new session.
    Task AddAsync(SessionSnapshot session, CancellationToken cancellationToken);

    // Replaces the stored session only if its version is unchanged.
    Task<bool> TryUpdateAsync(SessionSnapshot session, CancellationToken cancellationToken);
}
