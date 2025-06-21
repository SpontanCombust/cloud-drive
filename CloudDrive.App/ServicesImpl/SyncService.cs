using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using CloudDrive.App.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;


namespace CloudDrive.App.ServicesImpl
{
    public class SyncService : ISyncService
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly IUserSettingsService _userSettingsService;
        private readonly ILogger<SyncService> _logger;
        private readonly IBenchmarkService _benchmarkService;
        private readonly ILocalCommitedFileIndexService _localCommitedFileIndex;

        public SyncService(
            WebAPIClientFactory apiFactory, 
            IUserSettingsService userSettingsService, 
            ILogger<SyncService> logger,
            IBenchmarkService benchmarkService,
            ILocalCommitedFileIndexService fileIndex)
        {
            _apiFactory = apiFactory;
            _userSettingsService = userSettingsService;
            _logger = logger;
            _benchmarkService = benchmarkService;
            _localCommitedFileIndex = fileIndex;
        }

        public async Task SynchronizeAllFilesAsync()
        {
            var localIncomingFileIndex = App.Services.GetRequiredService<ILocalIncomingFileIndexService>();
            var remoteIncomingFileIndex = App.Services.GetRequiredService<IRemoteIncomingFileIndexService>();

            if (localIncomingFileIndex is null || remoteIncomingFileIndex is null)
            {
                _logger.LogError("Nie można przeprowadzić synchronizacji, ponieważ nie można uzyskać dostępu do indeksów plików.");
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Pełna synchronizacja");

            try
            {
                await remoteIncomingFileIndex.FetchAsync();
                localIncomingFileIndex.ScanWatchedFolder();

                var staging = new FileIndexStagingHelper();

                staging.Stage(
                    _localCommitedFileIndex.FindAll(), 
                    localIncomingFileIndex.FindAll(), 
                    remoteIncomingFileIndex.FindAll());


                // Faza 1: Usuwanie folderów i plików
                var foldersToRemoveFromRemote = staging.DeletedLocally()
                    .Where(set => set.LocalCommited.IsDirectory)
                    .Select(set => set.LocalCommited.GetWatchedFileSystemPath());
                var filesToRemoveFromRemote = staging.DeletedLocally()
                    .Where(set => !set.LocalCommited.IsDirectory)
                    .Select(set => set.LocalCommited.GetWatchedFileSystemPath());

                var foldersToRemoveLocally = staging.DeletedRemotely()
                    .Where(set => set.LocalCommited.IsDirectory)
                    .Select(set => set.LocalCommited.GetWatchedFileSystemPath());
                var filesToRemoveLocally = staging.DeletedRemotely()
                    .Where(set => !set.LocalCommited.IsDirectory)
                    .Select(set => set.LocalCommited.GetWatchedFileSystemPath());

                var syncTasks = new List<Task>();

                // Usuń foldery zdalnie
                foreach (var fsPath in foldersToRemoveFromRemote)
                {
                    syncTasks.Add(RemoveFoldersFromRemoteAsync(fsPath));
                }
                // Usuń pliki zdalnie
                foreach (var fsPath in filesToRemoveFromRemote)
                {
                    syncTasks.Add(RemoveFileFromRemoteAsync(fsPath));
                }


                // Usuń foldery lokalnie
                foreach (var fsPath in foldersToRemoveLocally)
                {
                    if (Directory.Exists(fsPath.Full))
                    {
                        try
                        {
                            Directory.Delete(fsPath.Full, true);
                            _logger.LogInformation($"Usunięto folder lokalnie: {fsPath.Full}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Błąd przy usuwaniu folderu lokalnie: {fsPath.Full}");
                        }
                    }
                }

                // Usuń pliki lokalnie
                foreach (var fsPath in filesToRemoveLocally)
                {
                    if (File.Exists(fsPath.Full))
                    {
                        try
                        {
                            File.Delete(fsPath.Full);
                            _logger.LogInformation($"Usunięto plik lokalnie: {fsPath.Full}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Błąd przy usuwaniu pliku lokalnie: {fsPath.Full}");
                        }
                    }
                }


                // zaczekaj aż faza 1 się zakończy
                await Task.WhenAll(syncTasks);
                syncTasks.Clear();

                //FIXME oddzielne obsługiwanie nowych do pobrania i zmodyfikowanych do pobrania
                // Faza 2: Dodawanie i aktualizacja folderów
                var foldersToDownload = staging.AddedRemotely()
                    .Where(set => set.RemoteIncoming.IsDirectory)
                    .Select(set => set.RemoteIncoming);
                var foldersToUpload = staging.AddedLocally()
                    .Where(set => set.LocalIncoming.IsDirectory)
                    .Select(set => set.LocalIncoming.GetWatchedFileSystemPath());
                var foldersToUpdate = staging.ModifiedLocally()
                    .Where(set => set.LocalIncoming.IsDirectory)
                    .Select(set => set.LocalIncoming.GetWatchedFileSystemPath());

                foreach (var incomingData in foldersToDownload)
                {
                    syncTasks.Add(DownloadActiveFolderFromRemoteAsync(incomingData));
                }

                foreach (var fsPath in foldersToUpload)
                {
                    syncTasks.Add(UploadNewFolderToRemoteAsync(fsPath));
                }

                foreach (var fsPath in foldersToUpdate)
                {
                    syncTasks.Add(UploadModifiedFolderToRemoteAsync(fsPath));
                }


                // zaczekaj aż faza 2 się zakończy
                await Task.WhenAll(syncTasks);
                syncTasks.Clear();


                // Faza 3: Dodawanie i aktualizacja plików
                var filesToDownload = staging.AddedRemotely()
                    .Where(set => !set.RemoteIncoming.IsDirectory)
                    .Select(set => set.RemoteIncoming);
                var filesToUpload = staging.AddedLocally()
                    .Where(set => !set.LocalIncoming.IsDirectory)
                    .Select(set => set.LocalIncoming.GetWatchedFileSystemPath());
                var filesToUpdate = staging.ModifiedLocally()
                    .Where(set => !set.LocalIncoming.IsDirectory)
                    .Select(set => set.LocalIncoming.GetWatchedFileSystemPath());

                foreach (var incomingData in filesToDownload)
                {
                    syncTasks.Add(DownloadActiveFileFromRemoteAsync(incomingData));
                }

                foreach (var fsPath in filesToUpload)
                {
                    syncTasks.Add(UploadNewFileToRemoteAsync(fsPath));
                }

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
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }


        public bool TryGetFileId(WatchedFileSystemPath path, out Guid fileId)
        {
            var indexEntry = _localCommitedFileIndex.FindByWatchedPath(path);
            if (indexEntry != null)
            {
                fileId = indexEntry.FileId;
                return true;
            }

            fileId = Guid.Empty;
            return false;
        }


        //Foldery

        public async Task UploadNewFolderRecursivelyAsync(WatchedFileSystemPath folderPath)
        {
            if (!folderPath.IsDirectory || !folderPath.Exists)
            {
                _logger.LogWarning("Ścieżka nie jest folderem lub nie istnieje: {Path}", folderPath.Full);
                return;
            }

            try
            {
                await UploadNewFolderToRemoteAsync(folderPath);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy tworzeniu folderu: {Path}, StatusCode: {StatusCode}, Response: {Response}",
                    folderPath.Full, ex.StatusCode, ex.Response);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd przy tworzeniu folderu: {Path}", folderPath.Full);
                return;
            }

            try
            {
                var directoryInfo = new DirectoryInfo(folderPath.Full);

                foreach (var dir in directoryInfo.GetDirectories())
                {
                    try
                    {
                        var subFolderPath = new WatchedFileSystemPath(dir.FullName, folderPath.WatchedFolder, true);
                        await UploadNewFolderRecursivelyAsync(subFolderPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Błąd przy rekurencyjnym wysyłaniu folderu: {Path}", dir.FullName);
                    }
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    if (file.Name.StartsWith("~$")) continue;

                    try
                    {
                        var filePath = new WatchedFileSystemPath(file.FullName, folderPath.WatchedFolder, false);
                        await UploadNewFileToRemoteAsync(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Błąd przy wysyłaniu pliku w folderze: {File}", file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przetwarzania zawartości folderu: {Path}", folderPath.Full);
            }
        }

        //FIXME osobne fukncje dla nowych i zmodyfikowanych zdalnie
        private async Task DownloadActiveFolderFromRemoteAsync(RemoteIncomingFileIndexEntry incomingRemoteEntry)
        {
            WatchedFileSystemPath path = incomingRemoteEntry.GetWatchedFileSystemPath();

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

                var commitedEntry = LocalCommitedFileIndexEntry.FromRemoteIncomingIndexEntryAndPath(incomingRemoteEntry, path);
                _localCommitedFileIndex.Insert(commitedEntry);

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
            if (!path.Exists)
            {
                throw new FileNotFoundException("Nie znaleziono folderu lokalnie", path.Full);
            }

            var oldIndexEntry = _localCommitedFileIndex.FindByWatchedPath(path);
            if (oldIndexEntry == null)
            {
                throw new InvalidOperationException("Nie znaleziono folderu w indeksie.");
            }


            var bench = _benchmarkService.StartBenchmark("Aktualizacja folderu", path.Relative);

            string parentDir = string.IsNullOrWhiteSpace(path.RelativeParentDir) ? "" : path.RelativeParentDir.Trim();

            try
            {
                var updateResp = await Api.UpdateDirectoryAsync(oldIndexEntry.FileId, parentDir, path.FileName);

                if (updateResp.Changed)
                {
                    var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(updateResp.NewFileVersionInfo, path);
                    _localCommitedFileIndex.Insert(newIndexEntry);

                    _logger.LogInformation("Zaktualizowano folder na serwerze: {Path}", path.Full);
                }
                else
                {
                    _logger.LogDebug("Nie zaktualizowano folderu na serwerze, bo nie wykryto zmian: {Path}", path.Full);
                }
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
            if (!newPath.Exists)
            {
                throw new FileNotFoundException("Nie znaleziono folderu lokalnie", newPath.Full);
            }

            var oldIndexEntry = _localCommitedFileIndex.FindByWatchedPath(oldPath);
            if (oldIndexEntry == null)
            {
                throw new InvalidOperationException("Nie znaleziono folderu w indeksie.");
            }


            var bench = _benchmarkService.StartBenchmark("Zmiana nazwy folderu", oldPath.Relative);

            string parentDir = string.IsNullOrWhiteSpace(newPath.RelativeParentDir) ? "" : newPath.RelativeParentDir.Trim();

            try
            {
                var resp = await Api.UpdateDirectoryAsync(oldIndexEntry.FileId, parentDir, newPath.FileName);

                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(resp.NewFileVersionInfo, newPath);
                _localCommitedFileIndex.Insert(newIndexEntry);

                _logger.LogInformation("Zaktualizowano folder na serwerze: {OldPath} -> {NewPath}", oldPath.Full, newPath.Full);


                foreach (var newSubfileVersionExt in resp.NewSubfileVersionInfosExt)
                {
                    var newSubfilePath = new WatchedFileSystemPath(
                        Path.Combine(newPath.WatchedFolder, newSubfileVersionExt.ClientFilePath()),
                        newPath.WatchedFolder,
                        newSubfileVersionExt.File.IsDir
                    );

                    var oldSubfileIndexEntry = _localCommitedFileIndex.Find(newSubfileVersionExt.File.FileId);
                    if (oldSubfileIndexEntry != null)
                    {
                        var newSubfileIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(newSubfileVersionExt.FileVersion, newSubfilePath);
                        _localCommitedFileIndex.Insert(newSubfileIndexEntry);

                        _logger.LogInformation("Zaktualizowano plik/folder na serwerze po zmianie nazwy folderu: {OldPath} -> {NewPath}",
                            oldSubfileIndexEntry.FullPath, newSubfilePath.Full);
                    }
                    else
                    {
                        _logger.LogWarning("Nie znaleziono lokalnie pliku/folderu, który powinien zostać zaktualizowany po zmianie nazwy folderu: {FileId}",
                            newSubfileVersionExt.File.FileId);
                    }
                }
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

                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(resp.FirstFileVersionInfo, path);
                _localCommitedFileIndex.Insert(newIndexEntry);

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
            var existingIndexEntry = _localCommitedFileIndex.FindByWatchedPath(path);
            if (existingIndexEntry == null)
            {
                _logger.LogWarning("Nie znaleziono folderu do usunięcia: {Path}", path.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Usuwanie folderu", path.Relative);

            try
            {
                var resp = await Api.DeleteDirectoryAsync(existingIndexEntry.FileId);
                _logger.LogInformation("Usunięto folder z serwera: {Path}", path.Full);

                _localCommitedFileIndex.Remove(existingIndexEntry.FileId);
                _logger.LogInformation("Usunięto informacje o folderze: {Path}", path.Full);


                var entriesToRemove = _localCommitedFileIndex
                    .FindInDirectory(existingIndexEntry.FileId)
                    .ToArray();

                foreach (var e in entriesToRemove)
                {
                    _localCommitedFileIndex.Remove(e.FileId);
                    _logger.LogInformation("Usunięto indeks dla pliku lub folderu wewnątrz usuniętego katalogu: {Path}", e.FullPath);
                }

                if (entriesToRemove.Length != resp.AffectedSubfiles.Count)
                {
                    _logger.LogWarning("Ilość usuniętych plików/folderów ({Count}) nie zgadza się z ilością zmienionych plików na serwerze ({ServerCount})",
                        entriesToRemove.Length, resp.AffectedSubfiles.Count);
                }
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd przy usuwaniu folderu: {Path}", path.Full);
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
            var oldIndexEntry = _localCommitedFileIndex.Find(fileId);
            if (oldIndexEntry != null)
            {
                _logger.LogDebug("Plik {} nie zostanie przywrócony, bo nadal istnieje w systemie", oldIndexEntry.FullPath);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Przywrócenie folderu");

            try
            {
                var restoredState = await Api.RestoreDirectoryAsync(fileId, null, null);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientFilePath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, true);

                // przywracanie folderu w systemie ogranicza się do jego stworzenia w systemie plików
                // dla niego samego nie trzeba nic więcej pobierać z serwera
                Directory.CreateDirectory(newFullPath);

                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(restoredState.ActiveFileVersionInfo, newFsPath);
                _localCommitedFileIndex.Insert(newIndexEntry);

                // przywracanie uprzedniej zawartości folderu nie jest obsługiwane

                _logger.LogInformation("Przywrócono folder z serwera: {Path}", newFsPath.Full);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy przywracaniu folderu: {FileId}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    fileId, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu folderu: {FileId}", fileId);
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

            try
            {
                var restoredState = await Api.RestoreDirectoryAsync(fileId, fileVersionId, null);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientFilePath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, true);

                // zapis odtworzonych informacji o wersji pliku
                // ta czynność jest wspólna dla wszystkich przypadków
                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(restoredState.ActiveFileVersionInfo, newFsPath);
                LocalCommitedFileIndexEntry? oldIndexEntry = _localCommitedFileIndex.Insert(newIndexEntry);

                if (oldIndexEntry != null)
                {
                    // jeśli folder już istnieje a poprzednia wersja miała inną ścieżkę, to przenosimy folder
                    if (!oldIndexEntry.FullPath.Equals(newFsPath.Full, StringComparison.OrdinalIgnoreCase))
                    {
                        Directory.CreateDirectory(newFsPath.FullParentDir);
                        Directory.Move(oldIndexEntry.FullPath, newFsPath.Full);

                        _logger.LogInformation("Przywrócono stan folderu z serwera: {OldPath} -> {NewPath}", oldIndexEntry.FullPath, newFsPath.Full);
                    }
                    // jeśli już istnieje a przywrócona wersja nie różni się ścieżką, to nie robimy nic w systemie plików
                    else
                    {
                        _logger.LogInformation("Przywrócono stan folderu z serwera: nie dokonano zmian dla {OldPath}", oldIndexEntry.FullPath);
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
                _logger.LogError(ex, "Błąd API przy przywracaniu folderu: {FileId}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    fileId, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu folderu: {FileId}", fileId);
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

                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(resp.FirstFileVersionInfo, path);
                _localCommitedFileIndex.Insert(newIndexEntry);

                _logger.LogInformation($"Wysłano plik z: {path.Full}");
            }
            catch (ApiException ex)
            {
                _logger.LogError("Błąd API (Upload file): {Path}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    path.Full, ex.StatusCode, ex.Response);
                throw;
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
        //FIXME osobne fukncje dla nowych i zmodyfikowanych zdalnie
        private async Task DownloadActiveFileFromRemoteAsync(RemoteIncomingFileIndexEntry incomingRemoteEntry)
        {
            WatchedFileSystemPath path = incomingRemoteEntry.GetWatchedFileSystemPath();

            var bench = _benchmarkService.StartBenchmark("Pobieranie pliku", path.Relative);

            try
            {
                var fileResponse = await Api.GetActiveFileVersionAsync(incomingRemoteEntry.FileId);

                Directory.CreateDirectory(path.FullParentDir);

                using (var fileStream = File.Create(path.Full))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);

                    _logger.LogInformation($"Pobrano plik do: {path.Full}");
                }

                var commitedEntry = LocalCommitedFileIndexEntry.FromRemoteIncomingIndexEntryAndPath(incomingRemoteEntry, path);
                _localCommitedFileIndex.Insert(commitedEntry);
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

        public static async Task<string> CalculateFileHash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var hashBytes = await MD5.HashDataAsync(stream);
            var hashStr = Convert.ToHexString(hashBytes);
            return hashStr;
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

            var oldIndexEntry = _localCommitedFileIndex.FindByWatchedPath(path);
            if (oldIndexEntry == null)
            {
                throw new InvalidOperationException("Nie znaleziono wersji w indeksie.");
            }

            // Obliczanie lokalnego hash i porównanie
            string localHash = await CalculateFileHash(path.Full);
            if (!string.IsNullOrEmpty(localHash) &&
                localHash.Equals(oldIndexEntry.Md5, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Plik nie zmienił się — pomijam upload: {Path}", path.Full);
                return;
            }

            var bench = _benchmarkService.StartBenchmark("Aktualizacja pliku", path.Relative);

            try
            {
                using var fileStream = File.OpenRead(path.Full);
                var fileParam = new FileParameter(fileStream, path.FileName, "application/octet-stream");

                var updateResp = await Api.UpdateFileAsync(oldIndexEntry.FileId, fileParam, path.RelativeParentDir);

                if (updateResp.Changed)
                {
                    var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(updateResp.NewFileVersionInfo, path);
                    _localCommitedFileIndex.Insert(newIndexEntry);
                    _logger.LogInformation("Zaktualizowano plik na serwerze: {Path}", path.Full);
                }
                else
                {
                    _logger.LogDebug("Nie zaktualizowano pliku na serwerze, bo nie wykryto zmian: {Path}", path.Full);
                }
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

            var oldIndexEntry = _localCommitedFileIndex.FindByWatchedPath(oldPath);
            if (oldIndexEntry == null)
            {
                throw new InvalidOperationException("Nie znaleziono pliku w indeksie.");
            }


            var bench = _benchmarkService.StartBenchmark("Zmiana nazwy pliku", oldPath.Relative);

            try
            {
                using var fileStream = File.OpenRead(newPath.Full);
                var fileParam = new FileParameter(fileStream, newPath.FileName, "application/octet-stream");

                var resp = await Api.UpdateFileAsync(oldIndexEntry.FileId, fileParam, newPath.RelativeParentDir);

                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(resp.NewFileVersionInfo, newPath);
                _localCommitedFileIndex.Insert(newIndexEntry);

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
            var indexEntry = _localCommitedFileIndex.FindByWatchedPath(path);
            if (indexEntry == null)
            {
                _logger.LogWarning("Nie znaleziono pliku do usunięcia: {Path}", path.Full);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Usuwanie pliku", path.Relative);

            try
            {
                await Api.DeleteFileAsync(indexEntry.FileId);

                _localCommitedFileIndex.Remove(indexEntry.FileId);

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
            var oldIndexEntry = _localCommitedFileIndex.Find(fileId);
            if (oldIndexEntry != null)
            {
                _logger.LogDebug("Plik {} nie zostanie przywrócony, bo nadal istnieje w systemie", oldIndexEntry.FullPath);
                return;
            }


            var bench = _benchmarkService.StartBenchmark("Przywrócenie pliku");

            try
            {
                var restoredState = await Api.RestoreFileAsync(fileId, null);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientFilePath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, false);


                var fileResponse = await Api.GetFileVersionAsync(fileId, restoredState.ActiveFileVersionInfo.VersionNr);

                Directory.CreateDirectory(newFsPath.FullParentDir);

                using (var fileStream = File.Create(newFsPath.Full))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);

                    _logger.LogInformation("Pobrano plik do: {Path}", newFsPath.Full);
                }

                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(restoredState.ActiveFileVersionInfo, newFsPath);
                _localCommitedFileIndex.Insert(newIndexEntry);

                _logger.LogInformation("Przywrócono stan pliku z serwera: {Path}", newFsPath.Relative);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy przywracaniu pliku: {FileId}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    fileId, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu pliku: {FileId}", fileId);
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

            try
            {
                var restoredState = await Api.RestoreFileAsync(fileId, fileVersionId);

                string watchedFolder = _userSettingsService.WatchedFolderPath ?? string.Empty;
                string newFullPath = Path.Combine(watchedFolder, restoredState.ActiveFileVersionInfo.ClientFilePath());
                var newFsPath = new WatchedFileSystemPath(newFullPath, watchedFolder, false);

                // zapis odtworzonych informacji o wersji pliku
                // ta czynność jest wspólna dla wszystkich przypadków
                var newIndexEntry = LocalCommitedFileIndexEntry.FromFileVersionAndPath(restoredState.ActiveFileVersionInfo, newFsPath);
                LocalCommitedFileIndexEntry? oldIndexEntry = _localCommitedFileIndex.Insert(newIndexEntry);

                // jeśli istnieje już ten plik, trzeba go usunąć
                if (oldIndexEntry != null)
                {
                    File.Delete(oldIndexEntry.FullPath);

                    _logger.LogInformation("Usunięto niechcianą wersję pliku: {OldPath}", oldIndexEntry.FullPath);
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
                _logger.LogError(ex, "Błąd API przy przywracaniu pliku: {FileId}\nStatusCode: {StatusCode}\nResponse: {Response}",
                    fileId, ex.StatusCode, ex.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przy przywracaniu pliku: {FileId}", fileId);
            }
            finally
            {
                _benchmarkService.StopBenchmark(bench);
            }
        }



        public WatchedFileSystemPath? FindWatchedFileSystemPathByFullPath(string rawFullPath)
        {
            var indexEntry = _localCommitedFileIndex.FindByRawFullPath(rawFullPath);
            if (indexEntry != null)
            {
                return new WatchedFileSystemPath(indexEntry.FullPath, indexEntry.WatchedFolderPath, indexEntry.IsDirectory);
            }

            return null;
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

