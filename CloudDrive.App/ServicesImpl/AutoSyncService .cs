using System;
using System.Threading;
using System.Threading.Tasks;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;

namespace CloudDrive.App.ServicesImpl
{
    public class AutoSyncService : IAutoSyncService, IDisposable
    {
        private readonly ISyncService _syncService;
        private readonly ILogger<AutoSyncService> _logger;
        private Timer? _timer;
        private bool _isRunning = false;
        private readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(10);

        public AutoSyncService(ISyncService syncService, ILogger<AutoSyncService> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }
        public void StartSync()
        {
            if (_timer != null)
            {
                _logger.LogWarning("AutoSyncService już działa.");
                return;
            }

            _logger.LogInformation("AutoSyncService uruchomiony. Synchronizacja co {Interval}.", _syncInterval);
            _timer = new Timer(SyncCallback, null, TimeSpan.Zero, _syncInterval);
        }

        public void StopSync()
        {
            _timer?.Dispose();
            _timer = null;
            _logger.LogInformation("AutoSyncService zatrzymany.");
        }

        private async void SyncCallback(object? state)
        {
            if (_isRunning)
            {
                _logger.LogDebug("Synchronizacja pominięta – poprzednia wciąż trwa.");
                return;
            }

            _isRunning = true;

            try
            {
                _logger.LogInformation("AutoSync: rozpoczęcie synchronizacji...");
                await _syncService.SynchronizeAllFilesAsync();
                _logger.LogInformation("AutoSync: zakończono synchronizację.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoSync: błąd podczas synchronizacji.");
            }
            finally
            {
                _isRunning = false;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
