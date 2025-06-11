using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Shapes;


namespace CloudDrive.App.ServicesImpl
{
    public class SyncService : ISyncService
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly IUserSettingsService _userSettingsService;
        private readonly ILogger<SyncService> _logger;

        private Dictionary<WatchedFileSystemPath, FileVersionDTO> _fileVersionState;

        public SyncService(WebAPIClientFactory apiFactory, IUserSettingsService userSettingsService, ILogger<SyncService> logger)
        {
            _apiFactory = apiFactory;
            _userSettingsService = userSettingsService;
            _logger = logger;

            _fileVersionState = new Dictionary<WatchedFileSystemPath, FileVersionDTO>();
        }

        public async Task SynchronizeAllFilesAsync()
        {
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
                    syncTasks.Add(DownloadLatestFolderFromRemoteAsync(fileId, fsPath));
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
                    syncTasks.Add(DownloadLatestFileFromRemoteAsync(fileId, fsPath));
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
                _logger.LogError(ex, "Błąd komunikacji z serwerem.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Brak dostępu do pliku lub folderu.");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Błąd wejścia/wyjścia.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił nieoczekiwany błąd podczas synchronizacji plików.");
                if (ex.InnerException != null)
                    _logger.LogError(ex.InnerException, "Wewnętrzny wyjątek.");
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
                    _logger.LogDebug("Dodaję folder do stanu: {FullPath}, isDir: {IsDirectory}, fileId: {FileId}",
                 path.Full, isDir, fv.FileId);
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

        public async Task UploadNewFolderRecursivelyAsync(WatchedFileSystemPath folderPath)
        {
            if (!folderPath.IsDirectory || !folderPath.Exists)
            {
                _logger.LogWarning("Ścieżka nie jest folderem lub nie istnieje: {Path}", folderPath.Full);
                return;
            }

            // 1. Utwórz folder na serwerze lub zaktualizuj jeśli już istnieje
            if (_fileVersionState.ContainsKey(folderPath))
            {
                await UploadModifiedFolderToRemoteAsync(folderPath);
            }
            else
            {
                await UploadNewFolderToRemoteAsync(folderPath);
            }

            // 2. Pobierz zawartość lokalnego katalogu
            var directoryInfo = new DirectoryInfo(folderPath.Full);
            var files = directoryInfo.GetFiles();
            var directories = directoryInfo.GetDirectories();

            // 3. Prześlij wszystkie pliki w folderze
            foreach (var fileInfo in files)
            {
                if (fileInfo.Name.StartsWith("~$")) continue; // pomiń pliki tymczasowe Office itp.

                var filePath = new WatchedFileSystemPath(
                    fullPath: fileInfo.FullName,
                    watchedFolder: folderPath.WatchedFolder,
                    isDirectory: false
                );

                if (_fileVersionState.ContainsKey(filePath))
                {
                    await UploadModifiedFileToRemoteAsync(filePath);
                }
                else
                {
                    await UploadNewFileToRemoteAsync(filePath);
                }
            }

            // 4. Rekurencyjnie prześlij wszystkie podfoldery
            foreach (var dirInfo in directories)
            {
                var subfolderPath = new WatchedFileSystemPath(
                    fullPath: dirInfo.FullName,
                    watchedFolder: folderPath.WatchedFolder,
                    isDirectory: true
                );

                await UploadNewFolderRecursivelyAsync(subfolderPath);
            }
        }

        //Foldery

        public async Task DownloadLatestFolderFromRemoteAsync(Guid folderId, WatchedFileSystemPath path)
        {
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
        }

        public async Task UploadModifiedFolderToRemoteAsync(WatchedFileSystemPath path)
        {
            if (path.FileName.StartsWith("~$"))
            {
                _logger.LogInformation("Pomijam tymczasowy plik Office: {Path}", path.Full);
                return;
            }

            if (!_fileVersionState.TryGetValue(path, out var version))
            {
                _logger.LogWarning("Nie znaleziono folderu do aktualizacji: {Path}", path.Full);
                return;
            }

            string? parentDir = string.IsNullOrWhiteSpace(path.RelativeParentDir) ? null : path.RelativeParentDir.Trim();

            _logger.LogDebug("DEBUG: UpdateDirectoryAsync({FileId}, {ParentDir}, {FolderName})",
                             version.FileId, parentDir, path.FileName);

            try
            {
                _logger.LogInformation("Aktualizuję folder na serwerze: {Path}, fileId: {FileId}, parentDir: '{ParentDir}', folderName: {FolderName}",
                    path.Full, version.FileId, parentDir, path.FileName);

                var resp = await Api.UpdateDirectoryAsync(version.FileId, parentDir, path.FileName);

                _fileVersionState[path] = resp.NewFileVersionInfo;
                _logger.LogInformation("Folder zaktualizowany pomyślnie: {Path}", path.Full);
            }
            catch (ApiException apiEx)
            {
                _logger.LogError(apiEx, "Błąd API przy aktualizacji folderu: {Path}, status code: {StatusCode}",
                    path.Full, apiEx.StatusCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd przy aktualizacji folderu: {Path}", path.Full);
                throw;
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
        }
        public async Task RemoveFoldersFromRemoteAsync(WatchedFileSystemPath path)
        {
            if (!_fileVersionState.TryGetValue(path, out var version))
            {
                _logger.LogWarning("Nie znaleziono folderu do usunięcia: {Path}", path.Full);
                return;
            }

            try
            {
                await Api.DeleteDirectoryAsync(version.FileId);
                _fileVersionState.Remove(path);

                _logger.LogInformation($"Usunięto folder z serwera: {path.Full}");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd przy usuwaniu folderu: {Path}", path.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny przy usuwaniu folderu: {Path}", path.Full);
                throw;
            }
        }
        //Pliki
        public async Task DeleteFileAsync(Guid fileId, WatchedFileSystemPath path)
        {
            try
            {
                await Api.DeleteFileAsync(fileId);
                _fileVersionState.Remove(path);
                _logger.LogInformation($"Usunięto plik z serwera: {path.Full}");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, $"Błąd przy usuwaniu pliku {path.Full} z serwera");
            }
        }
        public async Task UpdateFileAsync(Guid fileId, WatchedFileSystemPath path)
        {
            try
            {
                using var fileStream = File.OpenRead(path.Full);
                var fileParam = new FileParameter(fileStream, path.FileName, "application/octet-stream");

                var resp = await Api.UpdateFileAsync(fileId, fileParam, path.RelativeParentDir);

                // Użyj właściwości NewFileVersionInfo, by zaktualizować stan
                _fileVersionState[path] = resp.NewFileVersionInfo;

                _logger.LogInformation($"Zmodyfikowano plik: {path.Full}");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy modyfikacji pliku: {Path}", path.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny przy modyfikacji pliku: {Path}", path.Full);
                throw;
            }
        }

        // Pomocnicza metoda do obliczania hasha MD5
        private static string CalculateFileHash(string filePath)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = md5.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public async Task UploadNewFileToRemoteAsync(WatchedFileSystemPath path)
        {
            if (path.FileName.StartsWith("~$"))
            {
                _logger.LogInformation("Pomijam tymczasowy plik Office: {Path}", path.Full);
                return;
            }
            if (!path.Exists)
                throw new Exception($"Plik nie istnieje: {path.Full}");

            try
            {
                // Oblicz sumę kontrolną MD5 lokalnego pliku
                string localHash = CalculateFileHash(path.Full);

                // Sprawdź, czy mamy już wersję pliku na serwerze
                if (_fileVersionState.TryGetValue(path, out var fileVersion))
                {
                    if (fileVersion.Md5 == localHash)
                    {
                        _logger.LogInformation("Plik {Path} jest taki sam jak na serwerze — pomijam upload.", path.Full);
                        return;
                    }
                }

                // Upload pliku, bo plik nie istnieje na serwerze lub hash się różni
                using var fileStream = File.OpenRead(path.Full);
                var fileParam = new FileParameter(fileStream, path.FileName, "application/octet-stream");
                var resp = await Api.CreateFileAsync(fileParam, path.RelativeParentDir);

                // Zaktualizuj lokalny stan wersji pliku
                _fileVersionState[path] = resp.FirstFileVersionInfo;

                _logger.LogInformation($"Wysłano plik z: {path.Full}");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API podczas wysyłania nowego pliku: {Path}", path.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadNewFileToRemoteAsync nie powiódł się: {Path}", path.Full);
                throw;
            }
        }

        private async Task DownloadLatestFileFromRemoteAsync(Guid fileId, WatchedFileSystemPath path)
        {
            try
            {
                var fileResponse = await Api.GetLatestFileVersionAsync(fileId);

                Directory.CreateDirectory(path.FullParentDir);

                using (var fileStream = File.Create(path.Full))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);

                    _logger.LogInformation($"Pobrano plik do: {path.Full}");
                }
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Błąd API przy pobieraniu pliku: {Path}", path.Full);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ogólny przy pobieraniu pliku: {Path}", path.Full);
                throw;
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

            try
            {
                using var fileStream = File.OpenRead(path.Full);
                var fileParam = new FileParameter(fileStream, path.FileName, "application/octet-stream");

                var updatedVersion = await Api.UpdateFileAsync(version.FileId, fileParam, path.RelativeParentDir);

                _fileVersionState[path] = updatedVersion.NewFileVersionInfo;
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
        }

        public async Task RemoveFileFromRemoteAsync(WatchedFileSystemPath path)
        {
            if (!_fileVersionState.TryGetValue(path, out var version))
            {
                _logger.LogWarning("Nie znaleziono pliku do usunięcia: {Path}", path.Full);
                return;
            }

            try
            {
                await Api.DeleteFileAsync(version.FileId);
                _fileVersionState.Remove(path);
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


        private WebAPIClient Api
        {
            get
            {
                return _apiFactory.Create();
            }
        }
    }
}

