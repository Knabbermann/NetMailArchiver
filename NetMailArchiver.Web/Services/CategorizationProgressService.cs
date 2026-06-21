using System.Collections.Concurrent;
using NetMailArchiver.Services;

namespace NetMailArchiver.Web.Services
{
    /// <summary>
    /// Tracks progress of bulk categorization operations
    /// </summary>
    public interface ICategorizationProgressService
    {
        void UpdateProgress(string operationId, CategorizationProgress progress);
        CategorizationProgress? GetProgress(string operationId);
        void ClearProgress(string operationId);
    }

    public class CategorizationProgressService : ICategorizationProgressService
    {
        private readonly ConcurrentDictionary<string, CategorizationProgress> _progressMap = new();

        public void UpdateProgress(string operationId, CategorizationProgress progress)
        {
            _progressMap[operationId] = progress;
        }

        public CategorizationProgress? GetProgress(string operationId)
        {
            _progressMap.TryGetValue(operationId, out var progress);
            return progress;
        }

        public void ClearProgress(string operationId)
        {
            _progressMap.TryRemove(operationId, out _);
        }
    }
}
