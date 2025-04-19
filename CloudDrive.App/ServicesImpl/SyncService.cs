using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;


namespace CloudDrive.App.ServicesImpl
{
    public class SyncService : ISyncService
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly IUserSettingsService _userSettingsService;

        /// <summary>
        /// Maps path information of files on client's computer to their current version state received from the server 
        /// </summary>
        private Dictionary<WatchedFileSystemPath, FileVersionDTO> _fileVersionState;

        public SyncService(WebAPIClientFactory apiFactory, IUserSettingsService userSettingsService)
        {
            _apiFactory = apiFactory;
            _userSettingsService = userSettingsService;

            _fileVersionState = new Dictionary<WatchedFileSystemPath, FileVersionDTO>();
        }


        

        public async Task SynchronizeAllFilesAsync()
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
                        Path.Combine(watched, fv.ClientDirPath, fv.ClientFileName),
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

                //TODO use a logger
                Console.WriteLine($"Wysłano plik z: {path.Full}");
            }
            catch (ApiException ex)
            {
                throw new Exception(ex.Response);
            }
        }

        private async Task DownloadLatestFileAsync(Guid fileId, WatchedFileSystemPath path)
        {
            try
            {
                var fileResponse = await Api.GetLatestFileVersionAsync(fileId);

                using (var fileStream = File.Create(path.Full))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);
                    //TODO use a logger
                    Console.WriteLine($"Pobrano plik do: {path.Full}");
                }
            }
            catch (ApiException ex)
            {
                throw new Exception(ex.Response);
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
            var localFiles = Directory.GetFiles(directoryPath)
                .Select(f => new WatchedFileSystemPath(f, watchedFolderPath, Directory.Exists(f)))
                .ToHashSet();

            var subpaths = new HashSet<WatchedFileSystemPath>();

            foreach (var path in localFiles)
            {
                if (path.IsDirectory)
                {
                    subpaths.UnionWith(ScanDirectory(path.Full, watchedFolderPath));
                }
            }

            localFiles.UnionWith(subpaths);

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

