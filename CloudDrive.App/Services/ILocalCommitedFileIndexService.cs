using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    /// <summary>
    /// Service responsible for persistently storing information about synced files and directories.
    /// </summary>
    public interface ILocalCommitedFileIndexService
    {
        LocalCommitedFileIndexEntry? Insert(LocalCommitedFileIndexEntry entry);

        LocalCommitedFileIndexEntry? Find(Guid fileId);
        LocalCommitedFileIndexEntry? FindByWatchedPath(WatchedFileSystemPath path);
        LocalCommitedFileIndexEntry? FindByRawFullPath(string fullPath);
        IEnumerable<LocalCommitedFileIndexEntry> FindInDirectory(Guid directoryFileId);
        IEnumerable<LocalCommitedFileIndexEntry> FindAll();

        bool Remove(Guid fileId);
        bool RemoveByPath(WatchedFileSystemPath path);
    }
}
