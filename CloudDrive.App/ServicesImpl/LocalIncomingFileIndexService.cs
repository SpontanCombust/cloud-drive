using CloudDrive.App.Model;
using CloudDrive.App.Services;
using System.IO;

namespace CloudDrive.App.ServicesImpl
{
    internal class LocalIncomingFileIndexService : ILocalIncomingFileIndexService
    {
        private readonly IUserSettingsService _userSettings;

        private readonly List<LocalIncomingFileIndexEntry> _incomingIndex;

        public LocalIncomingFileIndexService(IUserSettingsService userSettings)
        {
            _userSettings = userSettings;
            _incomingIndex = new List<LocalIncomingFileIndexEntry>();
        }


        public void ScanWatchedFolder()
        {
            string watchedFolder = _userSettings.WatchedFolderPath;

            if (string.IsNullOrEmpty(watchedFolder) || !Directory.Exists(watchedFolder))
                throw new Exception("Ścieżka do obserwowanego folderu nie została ustawiona lub nie istnieje.");

            _incomingIndex.Clear();
            ScanDirectory(watchedFolder, watchedFolder);
        }

        private void ScanDirectory(string directoryPath, string watchedFolderPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);

            foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                var entry = new LocalIncomingFileIndexEntry
                {
                    FullPath = file.FullName,
                    WatchedFolderPath = watchedFolderPath,
                    IsDirectory = false,
                    LastModifiedDate = file.LastWriteTimeUtc
                };
                _incomingIndex.Add(entry);
            }

            foreach (var dir in directoryInfo.GetDirectories("*", SearchOption.AllDirectories))
            {
                var entry = new LocalIncomingFileIndexEntry
                {
                    FullPath = dir.FullName,
                    WatchedFolderPath = watchedFolderPath,
                    IsDirectory = true,
                    LastModifiedDate = dir.LastWriteTimeUtc
                };
                _incomingIndex.Add(entry);
            }
        }


        public IEnumerable<LocalIncomingFileIndexEntry> FindAll()
        {
            return _incomingIndex.AsReadOnly();
        }
    }
}
