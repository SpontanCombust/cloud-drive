using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    /// <summary>
    /// Service responsible for supplying information about the current state of the watched host file system.
    /// </summary>
    public interface ILocalIncomingFileIndexService
    {
        void ScanWatchedFolder();
        void ScanFolder(string fullFolderPath);

        IEnumerable<LocalIncomingFileIndexEntry> FindAll();
    }
}
