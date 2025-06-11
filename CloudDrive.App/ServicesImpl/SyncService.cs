using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using CloudDrive.App.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.IO;


namespace CloudDrive.App.ServicesImpl
{
    public class SyncService : ISyncService
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly IUserSettingsService _userSettingsService;
        private readonly ILogger<SyncService> _logger;
        private readonly IBenchmarkService _benchmarkService;

        private Dictionary<WatchedFileSystemPath, FileVersionDTO> _fileVersionState;

        public SyncService(
            WebAPIClientFactory apiFactory, 
            IUserSettingsService userSettingsService, 
            ILogger<SyncService> logger,
            IBenchmarkService benchmarkService)
        {
            _apiFactory = apiFactory;
            _userSettingsService = userSettingsService;
            _logger = logger;
            _benchmarkService = benchmarkService;

            _fileVersionState = new Dictionary<WatchedFileSystemPath, FileVersionDTO>();
        }

        public async Task SynchronizeAllFilesAsync()
        {
            var bench = _benchmarkService.StartBenchmark("Pełna synchronizacja");

            try
            {
                await FetchStateFromRemoteAsync();

                var localFsPaths = ScanWatchedFolder();

                var localFolders = localFsPaths.Where(p => p.IsDirectory).ToHashSet();
                var localFiles = localFsPaths.Where(p => !p.IsDirectory).ToHashSet();

                var syncedFsPaths = _fileVersionState.Keys.ToHashSet();

                var syncedFolders = syncedFsPaths.Where(p => p.IsDirectory).ToHashSet();
                var syncedFiles = syncedFsPaths.Where(p => !p.IsDirectory).ToHashSet();

                var syncTasks = new List<Task>();

                //foldery
                var foldersToDownload = syncedFolders.Except(localFolders);
                foreach (var fsPath in foldersToDownload)
                {
                    Guid fileId = _fileVersionState[fsPath].FileId;
                    syncTasks.Add(DownloadActiveFolderFromRemoteAsync(fileId, fsPath));
                }

                var foldersToUpload = localFolders.Except(syncedFolders);
                foreach (var fsPath in foldersToUpload)
                {
                    syncTasks.Add(UploadNewFolderToRemoteAsync(fsPath));
                }

                var foldersToRemove = syncedFolders.Except(localFolders);
                foreach (var fsPath in foldersToRemove)
                {
                    syncTasks.Add(RemoveFoldersFromRemoteAsync(fsPath));
                }

                var foldersToUpdate = syncedFolders.Intersect(localFolders);
                foreach (var fsPath in foldersToUpdate)
                {
                    syncTasks.Add(UploadModifiedFolderToRemoteAsync(fsPath));
                }

                //Pliki
                var filesToDownload = syncedFiles.Except(localFiles);
                foreach (var fsPath in filesToDownload)
                {
                    Guid fileId = _fileVersionState[fsPath].FileId;
                    syncTasks.Add(DownloadActiveFileFromRemoteAsync(fileId, fsPath));
                }

                var filesToUpload = localFiles.Except(syncedFiles);
                foreach (var fsPath in filesToUpload)
                {
                    syncTasks.Add(UploadNewFileToRemoteAsync(fsPath));
                }

                var filesToRemove = syncedFiles.Except(localFiles);
                foreach (var fsPath in filesToRemove)
                {
                    syncTasks.Add(RemoveFileFromRemoteAsync(fsPath));
                }

                var filesToUpdate = syncedFiles.Intersect(localFiles);
                foreach (var fsPath in filesToUpdate)
                {
                    syncTasks.Add(UploadModifiedFileToRemoteAsync(fsPath));
                }

                await Task.WhenAll(syncTasks);
                _logger.LogInformation("Zakończono pełną synchronizację!");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd komunikacji z serwerem: " + ex.Response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Brak dostępu do pliku lub folderu: " + ex.Message);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Błąd wejścia/wyjścia: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pojawił się błąd podczas synchronizacji plików.");

                _logger.LogError("Źródło błędu: {Source}", ex.Source);
                _logger.LogError("Ślad stosu: {StackTrace}", ex.StackTrace);
                _logger.LogError("Opis błędu: {Message}", ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Wewnętrzny wyjątek: {Message}", ex.InnerException.Message);
                    _logger.LogError("Ślad stosu wewnętrznego wyjątku: {StackTrace}", ex.InnerException.StackTrace);
                }
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        private async Task FetchStateFromRemoteAsync()
        {

            string? watched = _userSettingsService.WatchedFolderPath;

            if (string.IsNullOrEmpty(watched) || !Directory.Exists(watched))
                throw new Exception("Ścieżka do obserwowanego folderu nie została ustawiona lub nie istnieje.");

            var response = await Api.SyncAllAsync();
            _fileVersionState = new Dictionary<WatchedFileSystemPath, FileVersionDTO>();

            foreach (var fv in response.CurrentFileVersionsInfos)
            {
                bool isDir = fv.Md5 == null && fv.SizeBytes == null; // albo fv.IsDirectory, jeśli dodasz property

                var fullPath = System.IO.Path.Combine(watched, fv.ClientDirPath ?? "", fv.ClientFileName);
                var path = new WatchedFileSystemPath(fullPath, watched, isDir);

                if (!_fileVersionState.ContainsKey(path))
                {
                    _fileVersionState[path] = fv;
                }
                else
                {
                    _logger.LogWarning("Duplikat ścieżki podczas budowy stanu plików: {Path}", path.Full);
                }

                if (string.IsNullOrEmpty(fv.ClientFileName))
                {
                    _logger.LogWarning("Pominięto wpis z pustą nazwą pliku");
                    continue;
                }
            }
        }


        public bool IsDirectory(FileVersionDTO fv)
        {
            return fv.Md5 == null && fv.SizeBytes == null;
        }

        public bool TryGetFileId(WatchedFileSystemPath path, out Guid fileId)
        {
            if (_fileVersionState.TryGetValue(path, out var fileVersion))
            {
                fileId = fileVersion.FileId;
                return true;
            }

            fileId = Guid.Empty;
            return false;
        }


        //Foldery

        private async Task DownloadActiveFolderFromRemoteAsync(Guid folderId, WatchedFileSystemPath path)
        {
            var bench = _benchmarkService.StartBenchmark("Pobieranie folderu", path.Relative);

            try
            {
                if (!Directory.Exists(path.Full))
                {
                    Directory.CreateDirectory(path.Full);
                    _logger.LogInformation($"Utworzono lokalnie folder: {path.Full}");
                }
                else
                {
                    _logger.LogInformation($"Folder lokalny już istnieje: {path.Full}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd przy tworzeniu lokalnego folderu: {path.Full}");
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        public async Task UploadModifiedFolderToRemoteAsync(WatchedFileSystemPath path)
        {
            if (!_fileVersionState.TryGetValue(path, out var version))
            {
                _logger.LogWarning("Nie znaleziono folderu do aktualizacji: {Path}", path.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Aktualizacja folderu", path.Relative);

            string parentDir = string.IsNullOrWhiteSpace(path.RelativeParentDir) ? "" : path.RelativeParentDir.Trim();

            try
            {
                var resp = await Api.UpdateDirectoryAsync(version.FileId, parentDir, path.FileName);

                _fileVersionState[path] = resp.NewFileVersionInfo;

                _logger.LogInformation("Zaktualizowano folder na serwerze: {Path}", path.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy aktualizacji folderu: {Path}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    path.Full, ex.StatusCode, ex.Response);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy aktualizacji folderu: {Path}", path.Full);
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        public async Task UploadRenamedFolderToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath)
        {
            if (!_fileVersionState.TryGetValue(oldPath, out var version))
            {
                _logger.LogWarning("Nie znaleziono folderu do aktualizacji: {Path}", oldPath.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Zmiana nazwy folderu", oldPath.Relative);

            string parentDir = string.IsNullOrWhiteSpace(newPath.RelativeParentDir) ? "" : newPath.RelativeParentDir.Trim();

            try
            {
                var resp = await Api.UpdateDirectoryAsync(version.FileId, parentDir, newPath.FileName);

                _fileVersionState[newPath] = resp.NewFileVersionInfo;
                _fileVersionState.Remove(oldPath);

                _logger.LogInformation("Zaktualizowano folder na serwerze: {OldPath} -> {NewPath}", oldPath.Full, newPath.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy zmianie nazwy folderu folderu: {Path}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    oldPath.Full, ex.StatusCode, ex.Response);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy zmianie nazwy folderu {Path}", oldPath.Full);
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        public async Task UploadNewFolderToRemoteAsync(WatchedFileSystemPath path)
        {
            if (path.FileName.StartsWith("~$"))
            {
                _logger.LogInformation("Pomijam tymczasowy plik Office: {Path}", path.Full);
                return;
            }
            if (!path.Exists || !path.IsDirectory)
                throw new Exception($"Folder nie istnieje lub nie jest folderem: {path.Full}");


            var bench = _benchmarkService.StartBenchmark("Nowy folder", path.Relative);

            try
            {
                var resp = await Api.CreateDirectoryAsync(path.RelativeParentDir ?? "", path.FileName);

                _fileVersionState.Add(path, resp.FirstFileVersionInfo);

                _logger.LogInformation("Wysłano folder: {Path}", path.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy wysyłaniu folderu: {Path}", path.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny przy wysyłaniu folderu: {Path}", path.Full);
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        public async Task RemoveFoldersFromRemoteAsync(WatchedFileSystemPath path)
        {
            if (!_fileVersionState.TryGetValue(path, out var version))
            {
                _logger.LogWarning("Nie znaleziono folderu do usunięcia: {Path}", path.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Usuwanie folderu", path.Relative);

            try
            {
                await Api.DeleteDirectoryAsync(version.FileId);

                _fileVersionState.Remove(path);

                _logger.LogInformation($"Usunięto folder z serwera: {path.Full}");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, $"Błąd przy usuwaniu folderu: {path.Full}");
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        /// <summary>
        /// Przywraca folder ze stanu usuniętego na serwerze.
        /// </summary>
        public async Task RestoreFolderFromRemoteAsync(Guid fileId)
        {
            if (TryGetLocalFileInfoByFileId(fileId, out var oldFsPath, out _))
            {
                _logger.LogDebug("Folder {} nie zostanie przywrócony, bo nadal istnieje w systemie", oldFsPath.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Przywrócenie folderu");

            try
            {
                var restoredState = await Api.RestoreDirectoryAsync(fileId, null, null);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientPath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, true);

                _fileVersionState[newFsPath] = restoredState.ActiveFileVersionInfo;

                Directory.CreateDirectory(newFullPath);

                // przywracanie uprzedniej zawartości folderu nie jest obsługiwane

                _logger.LogInformation("Przywrócono folder z serwera: {Path}", newFsPath.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy przywracaniu folderu: {Path}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    oldFsPath.Full, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu folderu: {Path}", oldFsPath.Full);
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        /// <summary>
        /// Przywraca konkretną wersję folderu, wliczając sytuację jeśli jest on już usunięty
        /// </summary>
        public async Task RestoreFolderFromRemoteAsync(Guid fileId, Guid fileVersionId)
        {
            var bench = _benchmarkService.StartBenchmark("Przywrócenie wersji folderu");

            WatchedFileSystemPath? oldFsPath = null;
            FileVersionDTO? oldFileVersion = null;
            bool oldFolderFound = TryGetLocalFileInfoByFileId(fileId, out oldFsPath, out oldFileVersion);

            try
            {
                var restoredState = await Api.RestoreDirectoryAsync(fileId, fileVersionId, null);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientPath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, true);

                // zapis odtworzonych informacji o wersji pliku
                // ta czynność jest wspólna dla wszystkich przypadków
                _fileVersionState[newFsPath] = restoredState.ActiveFileVersionInfo;

                if (oldFolderFound)
                {
                    // jeśli folder już istnieje a poprzednia wersja miała inną ścieżkę, to przenosimy folder
                    if (!oldFsPath.Equals(newFsPath))
                    {
                        // przenosimy folder, więc trzeba usunąć informacje o starej ścieżce
                        _fileVersionState.Remove(oldFsPath);

                        Directory.Move(oldFsPath.Full, newFullPath);

                        _logger.LogInformation("Przywrócono stan folderu z serwera: {OldPath} -> {NewPath}", oldFsPath.Full, newFsPath.Full);
                    }
                    // jeśli już istnieje a przywrócona wersja nie różni się ścieżką, to nie robimy nic w systemie plików
                    else
                    {
                        _logger.LogInformation("Przywrócono stan folderu z serwera: nie dokonano zmian dla {OldPath}", oldFsPath.Full);
                    }
                }
                // jeśli folder nie istnieje, to tworzymy go
                else
                {
                    Directory.CreateDirectory(newFullPath);

                    _logger.LogInformation("Przywrócono folder z serwera: {Path}", newFsPath.Full);
                }
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy przywracaniu folderu: {Path}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    oldFsPath.Full, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu folderu: {Path}", oldFsPath.Full);
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }



        //Pliki

        public async Task UploadNewFileToRemoteAsync(WatchedFileSystemPath path)
        {
            if (path.FileName.StartsWith("~$"))
            {
                _logger.LogInformation("Pomijam tymczasowy plik Office: {Path}", path.Full);
                return;
            }

            if (!path.Exists)
                throw new Exception("Plik nie istnieje");


            var bench = _benchmarkService.StartBenchmark("Nowy plik", path.Relative);

            try
            {
                using var fileStream = File.OpenRead(path.Full);

                var fileParam = new FileParameter(fileStream, path.FileName, "application/octet-stream");
                var resp = await Api.CreateFileAsync(fileParam, path.RelativeParentDir);

                _fileVersionState.Add(path, resp.FirstFileVersionInfo);

                _logger.LogInformation($"Wysłano plik z: {path.Full}");
            }
            catch (ApiException ex)
            {
                throw new Exception(ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadNewFileToRemoteAsync nie powiódł się: {Path}", path.Full);
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        private async Task DownloadActiveFileFromRemoteAsync(Guid fileId, WatchedFileSystemPath path)
        {
            var bench = _benchmarkService.StartBenchmark("Pobieranie pliku", path.Relative);

            try
            {
                var fileResponse = await Api.GetActiveFileVersionAsync(fileId);

                Directory.CreateDirectory(path.FullParentDir);

                using (var fileStream = File.Create(path.Full))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);

                    _logger.LogInformation($"Pobrano plik do: {path.Full}");
                }
            }
            catch (ApiException ex)
            {
                throw new Exception(ex.Response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        public async Task UploadModifiedFileToRemoteAsync(WatchedFileSystemPath path)
        {
            if (path.FileName.StartsWith("~$"))
            {
                _logger.LogInformation("Pomijam tymczasowy plik Office: {Path}", path.Full);
                return;
            }
            if (!path.Exists)
            {
                throw new FileNotFoundException("Nie znaleziono pliku lokalnie", path.Full);
            }

            if (!_fileVersionState.TryGetValue(path, out var version))
            {
                throw new InvalidOperationException("Nie znaleziono wersji pliku na serwerze.");
            }


            var bench = _benchmarkService.StartBenchmark("Aktualizacja pliku", path.Relative);

            try
            {
                using var fileStream = File.OpenRead(path.Full);
                var fileParam = new FileParameter(fileStream, path.FileName, "application/octet-stream");

                var updatedVersion = await Api.UpdateFileAsync(version.FileId, fileParam, path.RelativeParentDir);

                _fileVersionState[path] = updatedVersion.NewFileVersionInfo;

                _logger.LogInformation("Zaktualizowano plik na serwerze: {Path}", path.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy aktualizacji pliku: {Path}", path.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny przy modyfikacji pliku: {Path}", path.Full);
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        public async Task UploadRenamedFileToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath)
        {
            if (newPath.FileName.StartsWith("~$"))
            {
                _logger.LogInformation("Pomijam tymczasowy plik Office: {Path}", newPath.Full);
                return;
            }

            if (!newPath.Exists)
            {
                throw new FileNotFoundException("Nie znaleziono pliku lokalnie", newPath.Full);
            }

            if (!_fileVersionState.TryGetValue(oldPath, out var version))
            {
                throw new InvalidOperationException("Nie znaleziono wersji pliku na serwerze.");
            }


            var bench = _benchmarkService.StartBenchmark("Zmiana nazwy pliku", oldPath.Relative);

            try
            {
                using var fileStream = File.OpenRead(newPath.Full);
                var fileParam = new FileParameter(fileStream, newPath.FileName, "application/octet-stream");

                var updatedVersion = await Api.UpdateFileAsync(version.FileId, fileParam, newPath.RelativeParentDir);

                _fileVersionState[newPath] = updatedVersion.NewFileVersionInfo;
                _fileVersionState.Remove(oldPath);

                _logger.LogInformation("Zaktualizowano plik na serwerze: {OldPath} -> {NewPath}", oldPath.Full, newPath.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy zmianie nazwy pliku: {Path}", oldPath.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny przy zmianie nazwy pliku: {Path}", oldPath.Full);
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        public async Task RemoveFileFromRemoteAsync(WatchedFileSystemPath path)
        {
            if (!_fileVersionState.TryGetValue(path, out var version))
            {
                _logger.LogWarning("Nie znaleziono pliku do usunięcia: {Path}", path.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Usuwanie pliku", path.Relative);

            try
            {
                await Api.DeleteFileAsync(version.FileId);

                _fileVersionState.Remove(path);

                _logger.LogInformation("Usunięto plik na serwerze: {Path}", path.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy usuwaniu pliku: {Path}", path.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny przy usuwaniu pliku: {Path}", path.Full);
                throw;
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        /// <summary>
        /// Przywraca plik ze stanu usuniętego na serwerze
        /// </summary>
        public async Task RestoreFileFromRemoteAsync(Guid fileId)
        {
            if (TryGetLocalFileInfoByFileId(fileId, out var oldFsPath, out _))
            {
                _logger.LogDebug("Plik {} nie zostanie przywrócony, bo nadal istnieje w systemie", oldFsPath.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Przywrócenie pliku");

            try
            {
                var restoredState = await Api.RestoreFileAsync(fileId, null);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientPath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, false);

                _fileVersionState[newFsPath] = restoredState.ActiveFileVersionInfo;


                var fileResponse = await Api.GetFileVersionAsync(fileId, restoredState.ActiveFileVersionInfo.VersionNr);

                Directory.CreateDirectory(newFsPath.FullParentDir);

                using (var fileStream = File.Create(newFsPath.Full))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);

                    _logger.LogInformation("Pobrano plik do: {Path}", newFsPath.Full);
                }

                _logger.LogInformation("Przywrócono stan pliku z serwera: {Path}", newFsPath.Relative);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy przywracaniu pliku: {Path}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    oldFsPath.Full, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu pliku: {Path}", oldFsPath.Full);
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }

        /// <summary>
        /// Przywraca konkretną wersję pliku, wliczając sytuację jeśli jest on już usunięty
        /// </summary>
        public async Task RestoreFileFromRemoteAsync(Guid fileId, Guid fileVersionId)
        {
            var bench = _benchmarkService.StartBenchmark("Przywrócenie wersji pliku");

            WatchedFileSystemPath? oldFsPath = null;
            FileVersionDTO? oldFileVersion = null;
            bool oldFileFound = TryGetLocalFileInfoByFileId(fileId, out oldFsPath, out oldFileVersion);

            try
            {
                var restoredState = await Api.RestoreFileAsync(fileId, fileVersionId);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientPath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, true);

                // zapis odtworzonych informacji o wersji pliku
                // ta czynność jest wspólna dla wszystkich przypadków
                _fileVersionState[newFsPath] = restoredState.ActiveFileVersionInfo;


                // jeśli istnieje już ten plik, trzeba go usunąć
                if (oldFileFound)
                {
                    _fileVersionState.Remove(oldFsPath);

                    File.Delete(oldFsPath.Full);

                    _logger.LogInformation("Usunięto niechcianą wersję pliku: {OldPath}", oldFsPath.Full);
                }


                // przywracamy oczekiwaną wersję pliku
                var fileResponse = await Api.GetFileVersionAsync(fileId, restoredState.ActiveFileVersionInfo.VersionNr);

                Directory.CreateDirectory(newFsPath.FullParentDir);

                using (var fileStream = File.Create(newFsPath.Full))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);

                    _logger.LogInformation("Pobrano plik do: {Path}", newFsPath.Full);
                }

                _logger.LogInformation("Przywrócono stan pliku z serwera: {Path}", newFsPath.Relative);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy przywracaniu pliku: {Path}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    oldFsPath.Full, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu pliku: {Path}", oldFsPath.Full);
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }



        private HashSet<WatchedFileSystemPath> ScanWatchedFolder()
        {
            string? watched = _userSettingsService.WatchedFolderPath;

            if (string.IsNullOrEmpty(watched) || !Directory.Exists(watched))
                throw new Exception("Ścieżka do obserwowanego folderu nie została ustawiona lub nie istnieje.");

            return ScanDirectory(watched, watched);
        }

        private HashSet<WatchedFileSystemPath> ScanDirectory(string directoryPath, string watchedFolderPath)
        {
            var localFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                .Select(f => new WatchedFileSystemPath(f, watchedFolderPath, false))
                .ToHashSet();

            var localDirs = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories)
                .Select(f => new WatchedFileSystemPath(f, watchedFolderPath, true))
                .ToHashSet();

            return localFiles.Union(localDirs).ToHashSet();
        }


        private bool TryGetLocalFileInfoByFileId(Guid fileId, out WatchedFileSystemPath path, out FileVersionDTO fv)
        {
            var q = _fileVersionState.Where(kv => kv.Value.FileId == fileId);
            if (q.Any())
            {
                var kv = q.First();
                path = kv.Key;
                fv = kv.Value;
                return true;
            }
            else
            {
                path = null!;
                fv = null!;
                return false;
            }
        }


        private WebAPIClient Api
        {
            get
            {
                return _apiFactory.Create();
            }
        }
    }
}

