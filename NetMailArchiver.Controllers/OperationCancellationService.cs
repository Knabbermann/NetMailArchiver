using System.Collections.Concurrent;

namespace NetMailArchiver.Services
{
    /// <summary>
    /// Manages cancellation tokens for long-running operations
    /// </summary>
    public interface IOperationCancellationService
    {
        CancellationToken GetOrCreateToken(string operationId);
        void CancelOperation(string operationId);
        bool IsOperationRunning(string operationId);
        void CompleteOperation(string operationId);
    }

    public class OperationCancellationService : IOperationCancellationService
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _operations = new();

        public CancellationToken GetOrCreateToken(string operationId)
        {
            var cts = _operations.GetOrAdd(operationId, _ => new CancellationTokenSource());
            return cts.Token;
        }

        public void CancelOperation(string operationId)
        {
            if (_operations.TryRemove(operationId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        public bool IsOperationRunning(string operationId)
        {
            return _operations.ContainsKey(operationId);
        }

        public void CompleteOperation(string operationId)
        {
            if (_operations.TryRemove(operationId, out var cts))
            {
                cts.Dispose();
            }
        }
    }
}
