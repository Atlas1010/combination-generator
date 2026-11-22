namespace CombinationGenerator.Application.Abstractions;

// Serializes concurrent operations on a single session.
public interface ISessionLock
{
    ValueTask<IAsyncDisposable> AcquireAsync(Guid sessionId, CancellationToken cancellationToken);
}
