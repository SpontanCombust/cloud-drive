using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace CloudDrive.App.ServicesImpl
{
    public class AutoSyncService : IAutoSyncService, IDisposable
    {
        private readonly ISyncService _syncService;
        private readonly ILogger<AutoSyncService> _logger;
        private readonly IUserSettingsService _userSettings;

        private Timer? _timer;
        private bool _isRunning = false;
        private DateTime? _lastSuccessfulSync;

        public AutoSyncService(ISyncService syncService, ILogger<AutoSyncService> logger, IUserSettingsService userSettings)
        {
            _syncService = syncService;
            _logger = logger;
            _userSettings = userSettings;
        }

        public void StartSync()
        {
            if (_timer != null)
            {
                _logger.LogWarning("AutoSyncService działa.");
                return;
            }

            TimeSpan interval = TimeSpan.FromSeconds(_userSettings.SyncIntervalSeconds);

            _logger.LogInformation("AutoSyncService uruchomiony. Synchronizacja co {Interval}.", interval);
            _timer = new Timer(SyncCallback, null, TimeSpan.Zero, interval);
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

            if (!IsInternetAvailable())
            {
                _logger.LogWarning("Brak połączenia z internetem – synchronizacja pominięta.");
                return;
            }

            _isRunning = true;

            try
            {
                _logger.LogInformation("AutoSync: rozpoczęcie synchronizacji o {Time}...", DateTime.Now);
                await _syncService.SynchronizeAllFilesAsync();
                _lastSuccessfulSync = DateTime.Now;
                _logger.LogInformation("AutoSync: zakończono synchronizację o {Time}.", _lastSuccessfulSync);
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

        private bool IsInternetAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Błąd sprawdzania połączenia internetowego.");
                return false;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
