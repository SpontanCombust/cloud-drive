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

        public void ScanFolder(string fullFolderPath)
        {
            string watchedFolder = _userSettings.WatchedFolderPath;

            if (string.IsNullOrEmpty(fullFolderPath) || !Directory.Exists(fullFolderPath))
                throw new Exception("Podana ścieżka do folderu nie istnieje.");
            if (string.IsNullOrEmpty(watchedFolder) || !Directory.Exists(watchedFolder))
                throw new Exception("Ścieżka do obserwowanego folderu nie została ustawiona lub nie istnieje.");
            if (!fullFolderPath.StartsWith(watchedFolder, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Podany folder nie jest częścią obserwowanego folderu.");

            _incomingIndex.Clear();
            ScanDirectory(fullFolderPath, watchedFolder);
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
