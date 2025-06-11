using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<string, bool> _pathTypeCache = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastSyncTime = new();
        private readonly TimeSpan _syncThrottle = TimeSpan.FromSeconds(2);
        public FileSystemSyncWatcher(ISyncService syncService, IUserSettingsService settings, ILogger<FileSystemSyncWatcher> logger)
        {
            _syncService = syncService;
            _watchedFolder = settings.WatchedFolderPath ?? throw new ArgumentException("Ścieżka nie została ustawiona.");
            _logger = logger;

            if (!Directory.Exists(_watchedFolder))
            {
                throw new DirectoryNotFoundException($"Folder {_watchedFolder} nie istnieje.");
            }

            _watcher = new FileSystemWatcher(_watchedFolder)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
            };

            _watcher.Created += OnCreated;
            _watcher.Changed += OnChanged;
            _watcher.Deleted += OnDeleted;
        }

        public void Start()
        {
            try
            {
                if (!_watcher.EnableRaisingEvents)
                {
                    _watcher.EnableRaisingEvents = true;
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
                Console.WriteLine($"Błąd przy starcie nasłuchiwania folderu: {_watchedFolder}\n{ex}");
            }
        }

        public void Stop()
        {
            try
            {
                if (_watcher.EnableRaisingEvents)
                {
                    _watcher.EnableRaisingEvents = false;
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
                _logger.LogError(ex, "Błąd przy zakończeniu nasłuchiwania folderu: {Folder}", _watchedFolder);
                Console.WriteLine($"Błąd przy zakończeniu nasłuchiwania folderu: {_watchedFolder}\n{ex}");
            }
        }

        private bool ShouldSync(string path)
        {
            var now = DateTime.UtcNow;
            if (_lastSyncTime.TryGetValue(path, out var lastTime))
            {
                if (now - lastTime < _syncThrottle)
                    return false;
            }

            _lastSyncTime[path] = now;
            return true;
        }

        public void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (!ShouldSync(e.FullPath)) return;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500); // trochę poczekaj, by plik/folder się ustabilizował
                    bool isDir = Directory.Exists(e.FullPath);
                    var path = new WatchedFileSystemPath(e.FullPath, _watchedFolder, isDir);

                    _pathTypeCache[e.FullPath] = isDir;

                    if (isDir)
                    {
                        // nowy folder - synchronizuj go
                        if (_syncService.TryGetFileId(path, out _))
                        {
                            await _syncService.UploadNewFolderRecursivelyAsync(path);
                            _logger.LogInformation("Zaktualizowano folder rekurencyjnie na serwerze: {Path}", path.Full);
                        }
                        else
                        {
                            await _syncService.UploadNewFolderRecursivelyAsync(path);
                            _logger.LogInformation("Dodano nowy folder rekurencyjnie na serwerze: {Path}", path.Full);
                        }
                    }
                    else
                    {
                        if (_syncService.TryGetFileId(path, out _))
                        {
                            await _syncService.UploadModifiedFileToRemoteAsync(path);
                            _logger.LogInformation("Zaktualizowano plik na serwerze: {Path}", path.Full);
                        }
                        else
                        {
                            await _syncService.UploadNewFileToRemoteAsync(path);
                            _logger.LogInformation("Dodano nowy plik na serwerze: {Path}", path.Full);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "(Dodawanie) Błąd synchronizacji: {Path}", e.FullPath);
                }
            });
        }
        public void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (!ShouldSync(e.FullPath)) return;
            Task.Run(async () =>
            {
                try
                {
                    bool wasDir = _pathTypeCache.TryGetValue(e.FullPath, out var isDir) && isDir;
                    var path = new WatchedFileSystemPath(e.FullPath, _watchedFolder, wasDir);

                    if (wasDir)
                    {
                        await _syncService.RemoveFoldersFromRemoteAsync(path);
                        _logger.LogInformation("Usunięto folder na serwerze: {Path}", path.Full);
                    }
                    else
                    {
                        await _syncService.RemoveFileFromRemoteAsync(path);
                        _logger.LogInformation("Usunięto plik na serwerze: {Path}", path.Full);
                    }

                    _pathTypeCache.TryRemove(e.FullPath, out _);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "(Usuwanie) Błąd synchronizacji: {Path}", e.FullPath);
                }
            });
        }
        public void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (!ShouldSync(e.FullPath)) return;
            Task.Run(async () =>
            {
                try
                {
                    bool isDir = Directory.Exists(e.FullPath);
                    _pathTypeCache[e.FullPath] = isDir;
                    var path = new WatchedFileSystemPath(e.FullPath, _watchedFolder, isDir);

                    if (isDir)
                    {
                        if (_syncService.TryGetFileId(path, out _))
                        {
                            await _syncService.UploadModifiedFolderToRemoteAsync(path);
                            _logger.LogInformation("Zaktualizowano folder na serwerze: {Path}", path.Full);
                        }
                    }
                    else
                    {
                        if (_syncService.TryGetFileId(path, out _))
                        {
                            await _syncService.UploadModifiedFileToRemoteAsync(path);
                            _logger.LogInformation("Zaktualizowano plik na serwerze: {Path}", path.Full);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "(Modyfikowanie) Błąd synchronizacji: {Path}", e.FullPath);
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

