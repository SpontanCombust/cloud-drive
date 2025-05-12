using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;


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
                var syncedFsPaths = _fileVersionState.Keys.ToHashSet();
                var syncTasks = new List<Task>();


                var filesToDownload = syncedFsPaths.Except(localFsPaths);

                foreach (var fsPath in filesToDownload)
                {
                    Guid fileId = _fileVersionState[fsPath].FileId;
                    syncTasks.Add(DownloadLatestFileAsync(fileId, fsPath));
                }


                var filesToUpload = localFsPaths.Except(syncedFsPaths);

                foreach (var fsPath in filesToUpload)
                {
                    syncTasks.Add(UploadFileAsync(fsPath));
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
                _logger.LogError(ex, "Błą wejścia/wyjścia");
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
        }

        // Pobieranie metadanych z serwera (pliki)
        private async Task FetchStateFromRemoteAsync()
        {
            string? watched = _userSettingsService.WatchedFolderPath;

            if (string.IsNullOrEmpty(watched) || !Directory.Exists(watched))
                throw new Exception("Ścieżka do obserwowanego folderu nie została ustawiona lub nie istnieje.");

            var response = await Api.SyncAllAsync();
            _fileVersionState = response.CurrentFileVersionsInfos.ToDictionary(
                fv => new WatchedFileSystemPath(
                        Path.Combine(watched, fv.ClientDirPath ?? "", fv.ClientFileName),
                        watched,
                        false //TODO zmienić gdy serwer będzie wspierać synchronizację folderów
                    ),
                fv => fv
            );
        }

        private async Task UploadFileAsync(WatchedFileSystemPath path)
        {
            if (!path.Exists)
                throw new Exception("Plik nie istnieje");

            try
            {
                using var fileStream = File.OpenRead(path.Full);

                // Tworzymy obiekt FileParameter, który zawiera plik do przesłania
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
                throw new Exception(ex.Message);
            }
        }

        private async Task DownloadLatestFileAsync(Guid fileId, WatchedFileSystemPath path)
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
                throw new Exception(ex.Response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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

            //var localDirs = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories)
            //    .Select(f => new WatchedFileSystemPath(f, watchedFolderPath, true))
            //    .ToHashSet();

            return localFiles;
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

