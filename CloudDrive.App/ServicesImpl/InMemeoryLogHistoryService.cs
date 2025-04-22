using CloudDrive.App.Model;
using CloudDrive.App.Services;
using System.Collections.Concurrent;

namespace CloudDrive.App.ServicesImpl
{
    public class InMemeoryLogHistoryService : ILogHistoryService, IDisposable
    {
        private readonly ILogRelayService _logRelayService;
        
        private readonly int DEFAULT_CAPACITY = 100;
        private readonly ConcurrentQueue<LogMessageEventArgs> _logHistory = new();
        private int _capacity;
        

        public InMemeoryLogHistoryService(ILogRelayService logRelayService)
        {
            _logRelayService = logRelayService;
            _capacity = DEFAULT_CAPACITY;

            _logRelayService.LogAdded += OnLogRelayed;
        }


        public int Capacity
        {
            get => _capacity;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Capacity cannot be lesser than zero");

                _capacity = value;
            }
        }

        public void ClearHistory()
        {
            _logHistory.Clear();
        }

        public IReadOnlyCollection<LogMessageEventArgs> GetHistory()
        {
            return _logHistory;
        }


        public void Dispose()
        {
            _logRelayService.LogAdded -= OnLogRelayed;
        }


        private void AddToHistory(LogMessageEventArgs ev)
        {
            while (_logHistory.Count >= _capacity)
            {
                _logHistory.TryDequeue(out _);
            }

            _logHistory.Enqueue(ev);
        }

        private void OnLogRelayed(object? sender, LogMessageEventArgs e)
        {
            AddToHistory(e);
        }
    }
}
