using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ISyncService
    {
        Task SynchronizeAllFilesAsync();
        Task UploadNewFolderRecursivelyAsync(WatchedFileSystemPath folderPath);
        Task UploadNewFileToRemoteAsync(WatchedFileSystemPath path);
        Task UploadModifiedFileToRemoteAsync(WatchedFileSystemPath path);
        Task UploadRenamedFileToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task RemoveFileFromRemoteAsync(WatchedFileSystemPath path);
        Task RestoreFileFromRemoteAsync(Guid fileId);
        Task RestoreFileFromRemoteAsync(Guid fileId, Guid fileVersionId);

        Task UploadNewFolderToRemoteAsync(WatchedFileSystemPath path);
        Task UploadModifiedFolderToRemoteAsync(WatchedFileSystemPath path);
        Task UploadRenamedFolderToRemoteAsync(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task RemoveFoldersFromRemoteAsync(WatchedFileSystemPath path);
        Task RestoreFolderFromRemoteAsync(Guid fileId);
        Task RestoreFolderFromRemoteAsync(Guid fileId, Guid fileVersionId);

        bool TryGetFileId(WatchedFileSystemPath path, out Guid fileId);
    }
}
