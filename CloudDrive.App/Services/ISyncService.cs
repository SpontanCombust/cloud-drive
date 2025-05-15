using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ISyncService
    {
        Task SynchronizeAllFilesAsync();
        Task UploadNewFileToRemoteAsync(WatchedFileSystemPath path);
        Task UploadModifiedFileToRemoteAsync(WatchedFileSystemPath path);
        Task RemoveFileFromRemoteAsync(WatchedFileSystemPath path);
    }
}
