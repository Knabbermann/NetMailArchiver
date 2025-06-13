using System.Collections.Concurrent;

namespace NetMailArchiver.Services
{
    public class ArchiveLockService
    {
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

        public async Task<IDisposable> AcquireLockAsync(Guid imapId, CancellationToken cancellationToken)
        {
            var semaphore = _locks.GetOrAdd(imapId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);

            return new Release(() => semaphore.Release());
        }

        private class Release(Action release) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;
                release();
                _disposed = true;
            }
        }
    }
}
