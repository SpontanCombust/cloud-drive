using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ISyncService
    {
        Task SynchronizeAllFilesAsync();
        Task UploadNewFileToRemoteAsync(WatchedFileSystemPath path);
        Task UploadModifiedFileToRemoteAsync(WatchedFileSystemPath path);
        Task RemoveFileFromRemoteAsync(WatchedFileSystemPath path);
        bool TryGetFileId(WatchedFileSystemPath path, out Guid fileId);
        bool IsDirectory(FileVersionDTO fv);
        Task DownloadLatestFolderFromRemoteAsync(Guid folderId, WatchedFileSystemPath path);
        Task UploadModifiedFolderToRemoteAsync(WatchedFileSystemPath path);
        Task UploadNewFolderToRemoteAsync(WatchedFileSystemPath path);
        Task RemoveFoldersFromRemoteAsync(WatchedFileSystemPath path);

    }
}
