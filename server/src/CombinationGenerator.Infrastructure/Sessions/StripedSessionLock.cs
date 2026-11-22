using CombinationGenerator.Application.Abstractions;

namespace CombinationGenerator.Infrastructure.Sessions;

// In-process session lock backed by a fixed pool of semaphores.
internal sealed class StripedSessionLock : ISessionLock, IDisposable
{
    private const int StripeCount = 256;
    private readonly SemaphoreSlim[] _stripes = Enumerable.Range(0, StripeCount).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async ValueTask<IAsyncDisposable> AcquireAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var stripe = _stripes[(sessionId.GetHashCode() & int.MaxValue) % StripeCount];
        await stripe.WaitAsync(cancellationToken);
        return new Releaser(stripe);
    }

    public void Dispose()
    {
        foreach (var stripe in _stripes)
        {
            stripe.Dispose();
        }
    }

    private sealed class Releaser(SemaphoreSlim stripe) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                stripe.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
