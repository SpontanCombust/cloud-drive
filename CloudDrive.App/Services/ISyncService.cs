using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ISyncService
    {
        Task SynchronizeAllFilesAsync();
        Task UploadFileAsync(WatchedFileSystemPath path);
    }
}
