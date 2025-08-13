using System.Collections.Concurrent;

namespace NetMailArchiver.Services
{
    public interface IArchiveProgressService
    {
        void SetProgress(string imapId, int progress);
        int GetProgress(string imapId);
        void RemoveProgress(string imapId);
        bool IsJobRunning(string imapId);
        void SetJobRunning(string imapId, bool isRunning);
        IEnumerable<string> GetActiveJobs();
    }

    public class ArchiveProgressService : IArchiveProgressService
    {
        private readonly ConcurrentDictionary<string, int> _progressDictionary = new();
        private readonly ConcurrentDictionary<string, bool> _activeJobs = new();

        public void SetProgress(string imapId, int progress)
        {
            _progressDictionary[imapId] = progress;
        }

        public int GetProgress(string imapId)
        {
            return _progressDictionary.TryGetValue(imapId, out var progress) ? progress : 0;
        }

        public void RemoveProgress(string imapId)
        {
            _progressDictionary.TryRemove(imapId, out _);
            _activeJobs.TryRemove(imapId, out _);
        }

        public bool IsJobRunning(string imapId)
        {
            return _activeJobs.TryGetValue(imapId, out var isRunning) && isRunning;
        }

        public void SetJobRunning(string imapId, bool isRunning)
        {
            if (isRunning)
            {
                _activeJobs[imapId] = true;
            }
            else
            {
                _activeJobs.TryRemove(imapId, out _);
            }
        }

        public IEnumerable<string> GetActiveJobs()
        {
            return _activeJobs.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
        }
    }
}