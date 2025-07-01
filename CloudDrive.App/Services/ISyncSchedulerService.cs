using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ISyncSchedulerService
    {
        bool IsBusy { get; }

        Task ScheduleSynchronizeAllFiles();
        Task ScheduleUploadNewFileToRemote(WatchedFileSystemPath path);
        Task ScheduleUploadModifiedFileToRemote(WatchedFileSystemPath path);
        Task ScheduleUploadRenamedFileToRemote(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task ScheduleRemoveFileFromRemote(WatchedFileSystemPath path);
        Task ScheduleRestoreFileFromRemote(Guid fileId);
        Task ScheduleRestoreFileFromRemote(Guid fileId, Guid fileVersionId);
        Task ScheduleUploadNewFolderRecursively(WatchedFileSystemPath folderPath);
        Task ScheduleUploadNewFolderToRemote(WatchedFileSystemPath path);
        Task ScheduleUploadModifiedFolderToRemote(WatchedFileSystemPath path);
        Task ScheduleUploadRenamedFolderToRemote(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath);
        Task ScheduleRemoveFoldersFromRemote(WatchedFileSystemPath path);
        Task ScheduleRestoreFolderFromRemote(Guid fileId);
        Task ScheduleRestoreFolderFromRemote(Guid fileId, Guid fileVersionId);
    }
}
