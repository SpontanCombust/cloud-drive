using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ISyncService
    {
        Task<bool> ShouldSynchronizeWithRemoteAsync();
        Task SynchronizeAllFilesAsync();
        Task UploadNewFolderRecursivelyAsync(WatchedFileSystemPath folderPath);
        Task<bool> UploadNewFileToRemoteAsync(WatchedFileSystemPath path);
        Task<bool> UploadModifiedFileToRemoteAsync(WatchedFileSystemPath path);
        Task UploadRenamedFileToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task<bool> RemoveFileFromRemoteAsync(WatchedFileSystemPath path);
        Task RestoreFileFromRemoteAsync(Guid fileId);
        Task RestoreFileFromRemoteAsync(Guid fileId, Guid fileVersionId);
        Task<bool> UploadNewFolderToRemoteAsync(WatchedFileSystemPath path);
        Task UploadModifiedFolderToRemoteAsync(WatchedFileSystemPath path);
        Task UploadRenamedFolderToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task<bool> RemoveFoldersFromRemoteAsync(WatchedFileSystemPath path);
        Task RestoreFolderFromRemoteAsync(Guid fileId);
        Task RestoreFolderFromRemoteAsync(Guid fileId, Guid fileVersionId);
        bool TryGetFileId(WatchedFileSystemPath path, out Guid fileId);
        WatchedFileSystemPath? FindWatchedFileSystemPathByFullPath(string rawFullPath);
    }
}
