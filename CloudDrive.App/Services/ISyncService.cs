using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ISyncService
    {
        Task SynchronizeAllFilesAsync();

        Task UploadNewFileToRemoteAsync(WatchedFileSystemPath path);
        Task UploadModifiedFileToRemoteAsync(WatchedFileSystemPath path);
        Task UploadRenamedFileToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task RemoveFileFromRemoteAsync(WatchedFileSystemPath path);

        Task UploadNewFolderToRemoteAsync(WatchedFileSystemPath path);
        Task UploadModifiedFolderToRemoteAsync(WatchedFileSystemPath path);
        Task UploadRenamedFolderToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task RemoveFoldersFromRemoteAsync(WatchedFileSystemPath path);

        bool TryGetFileId(WatchedFileSystemPath path, out Guid fileId);
    }
}
