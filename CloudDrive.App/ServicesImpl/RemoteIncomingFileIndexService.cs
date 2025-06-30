using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using CloudDrive.App.Utils;
using System.IO;

namespace CloudDrive.App.ServicesImpl
{
    internal class RemoteIncomingFileIndexService : IRemoteIncomingFileIndexService
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly IUserSettingsService _userSettingsService;

        private readonly List<RemoteIncomingFileIndexEntry> _incomingIndex;
        private DateTime? _serverTime;

        public RemoteIncomingFileIndexService(
            WebAPIClientFactory apiFactory, 
            IUserSettingsService userSettingsService)
        {
            _apiFactory = apiFactory;
            _userSettingsService = userSettingsService;

            _incomingIndex = new List<RemoteIncomingFileIndexEntry>();
            _serverTime = null;
        }


        public async Task FetchAsync()
        {
            string watchedFolder = _userSettingsService.WatchedFolderPath;
            if (string.IsNullOrEmpty(watchedFolder) || !Directory.Exists(watchedFolder))
            {
                throw new Exception("Ścieżka do obserwowanego folderu nie została ustawiona lub nie istnieje.");
            }

            _incomingIndex.Clear();

            var api = _apiFactory.Create();
            var resp = await api.SyncAllExtAsync(false);

            _incomingIndex.AddRange(resp.CurrentFileVersionsInfosExt
                .Select(fve => new RemoteIncomingFileIndexEntry
                {
                    FileId = fve.File.FileId,
                    FileModifiedDate = fve.File.ModifiedDate?.DateTime,
                    IsDirectory = fve.File.IsDir,
                    FileVersionId = fve.FileVersion.FileVersionId,
                    WatchedFolderPath = watchedFolder,
                    FullPath = Path.Combine(watchedFolder, fve.FileVersion.ClientFilePath()),
                    VersionNr = fve.FileVersion.VersionNr,
                    Md5 = fve.FileVersion.Md5,
                    SizeBytes = fve.FileVersion.SizeBytes,
                    VersionCreatedDate = fve.FileVersion.CreatedDate.DateTime
                }));
            _serverTime = resp.ServerTime.DateTime;
        }

        public DateTime? LastFetchServerTime()
        {
            return _serverTime;
        }


        public IEnumerable<RemoteIncomingFileIndexEntry> FindAll()
        {
            return _incomingIndex.AsReadOnly();
        }
    }
}
