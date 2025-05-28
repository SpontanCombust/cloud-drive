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
                EnableRaisingEvents = false, // Początkowo ustawiamy na false
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
            };

            _watcher.Created += OnCreated;
            _watcher.Changed += OnChanged;
            _watcher.Renamed += OnRenamed;
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
                Console.WriteLine($"Błąd przy starcie nasłuchiwania folderu: {_watchedFolder}. {ex.Message}");
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
                _logger.LogError(ex, "Błąd przy próbie zakończenia nasłuchiwania folderu: {Folder}", _watchedFolder);
                Console.WriteLine($"Błąd przy zakończeniu nasłuchiwania folderu: {_watchedFolder}. {ex.Message}");
            }
        }



        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Wydarzenie: OnCreated, {e.FullPath}");
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500); // trochę poczekaj, by plik/folder się ustabilizował
                    bool isDir = Directory.Exists(e.FullPath);
                    var path = new WatchedFileSystemPath(e.FullPath, _watchedFolder, isDir);

                    if (isDir)
                    {
                        // nowy folder - synchronizuj go
                        if (_syncService.TryGetFileId(path, out var fileId))
                        {
                            await _syncService.UploadModifiedFolderToRemoteAsync(path);
                        }
                        else
                        {
                            await _syncService.UploadNewFolderToRemoteAsync(path);
                            _logger.LogInformation("Dodano nowy folder na serwerze: {Path}", path.Full);
                        }
                    }
                    else
                    {
                        if (_syncService.TryGetFileId(path, out var fileId))
                        {
                            await _syncService.UploadModifiedFileToRemoteAsync(path);
                            _logger.LogInformation("Dodano nowy plik na serwer: {Path}", path.Full);
                        }
                        else
                        {
                            await _syncService.UploadNewFileToRemoteAsync(path);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "(Dodawanie) Błąd synchronizacji: {Path}", e.FullPath);
                }
            });
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Wydarzenie: OnDelete, {e.FullPath}");
            Task.Run(async () =>
            {
                try
                {
                    bool wasDir = false;
                    var path = new WatchedFileSystemPath(e.FullPath, _watchedFolder, isDirectory: false);

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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "(Usuwanie) Błąd synchronizacji usunięcia: {Path}", e.FullPath);
                }
            });
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Wydarzenie: OnChanged, {e.FullPath}");
            Task.Run(async () =>
            {
                try
                {
                    bool isDir = Directory.Exists(e.FullPath);
                    var path = new WatchedFileSystemPath(e.FullPath, _watchedFolder, isDir);

                    if (isDir)
                    {
                        if (_syncService.TryGetFileId(path, out var fileId))
                        {
                            await _syncService.UploadModifiedFolderToRemoteAsync(path);
                            _logger.LogInformation("Zaktualizowano folder na serwerze: {Path}", path.Full);
                        }
                    }
                    else
                    {
                        if (_syncService.TryGetFileId(path, out var fileId))
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

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"Wydarzenie: OnRenamed, {e.FullPath}");
            Task.Run(async () =>
            {
                try
                {
                    bool isDir = Directory.Exists(e.FullPath);
                    var oldPath = new WatchedFileSystemPath(e.OldFullPath, _watchedFolder, isDir);
                    var newPath = new WatchedFileSystemPath(e.FullPath, _watchedFolder, isDir);

                    if (isDir)
                    {
                        if (_syncService.TryGetFileId(oldPath, out var fileId))
                        {
                            await _syncService.UploadRenamedFolderToRemoteAsync(oldPath, newPath);
                            _logger.LogInformation("Zaktualizowano folder na serwerze: {Path}", oldPath.Full);
                        }
                    }
                    else
                    {
                        if (_syncService.TryGetFileId(oldPath, out var fileId))
                        {
                            await _syncService.UploadRenamedFileToRemoteAsync(oldPath, newPath);
                            _logger.LogInformation("Zaktualizowano plik na serwerze: {Path}", oldPath.Full);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "(Zmiana nazwy) Błąd synchronizacji {Path}", e.FullPath);
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

