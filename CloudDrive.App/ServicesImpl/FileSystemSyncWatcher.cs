using System;
using System.IO;
using System.Threading.Tasks;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;

namespace CloudDrive.App.ServicesImpl
{
    public class FileSystemSyncWatcher : IFileSystemWatcher, IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ISyncService _syncService;
        private readonly ILogger<FileSystemSyncWatcher> _logger;
        private readonly string _watchedFolder;

        private bool _disposed = false;

        public FileSystemSyncWatcher(ISyncService syncService, string watchedFolder, ILogger<FileSystemSyncWatcher> logger)
        {
            _syncService = syncService;
            _watchedFolder = watchedFolder;
            _logger = logger;

            if (!Directory.Exists(_watchedFolder))
            {
                throw new DirectoryNotFoundException($"Folder {_watchedFolder} nie istnieje.");
            }

            _watcher = new FileSystemWatcher(_watchedFolder)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = false, // Początkowo ustawiamy na false
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
            };

            _watcher.Created += OnCreated;
        }

        public void Start()
        {
            try
            {
                if (!_watcher.EnableRaisingEvents)
                {
                    _watcher.EnableRaisingEvents = true;  // Włączamy nasłuchiwanie
                    _logger.LogInformation("Obserwowanie folderu: {Folder}", _watchedFolder);
                    Console.WriteLine($"Obserwowanie folderu: {_watchedFolder}");
                }
                else
                {
                    _logger.LogWarning("Folder już jest obserwowany: {Folder}", _watchedFolder);
                    Console.WriteLine($"Folder już jest obserwowany: {_watchedFolder}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy starcie nasłuchiwania folderu: {Folder}", _watchedFolder);
                Console.WriteLine($"Błąd przy starcie nasłuchiwania folderu: {_watchedFolder}. {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                if (_watcher.EnableRaisingEvents)
                {
                    _watcher.EnableRaisingEvents = false; // Zatrzymujemy nasłuchiwanie
                    _logger.LogInformation("Zakończenie obserwowania folderu: {Folder}", _watchedFolder);
                    Console.WriteLine($"Zakończenie obserwowania folderu: {_watchedFolder}");
                }
                else
                {
                    _logger.LogWarning("Nasłuchiwanie już zostało zakończone dla folderu: {Folder}", _watchedFolder);
                    Console.WriteLine($"Nasłuchiwanie już zostało zakończone dla folderu: {_watchedFolder}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy próbie zakończenia nasłuchiwania folderu: {Folder}", _watchedFolder);
                Console.WriteLine($"Błąd przy zakończeniu nasłuchiwania folderu: {_watchedFolder}. {ex.Message}");
            }
        }

        public void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Wydarzenie: OnCreated, {e.FullPath}");
            Task.Run(async () =>
            {
                try
                {
                    var isDir = Directory.Exists(e.FullPath);
                    var path = new WatchedFileSystemPath(e.FullPath, _watchedFolder, isDir);

                    if (!isDir)
                    {
                        await _syncService.UploadFileAsync(path);
                        _logger.LogInformation("Zsynchronizowano nowy plik: {Path}", path.Full);
                    }
                    else
                    {
                        _logger.LogInformation("Nowy folder: {Path}", path.Full);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd synchronizacji folderu: {Path}", e.FullPath);
                }
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _watcher.Dispose();
                _disposed = true;
            }
        }
    }
}

