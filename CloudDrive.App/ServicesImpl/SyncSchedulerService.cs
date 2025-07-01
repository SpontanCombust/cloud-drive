using CloudDrive.App.Model;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CloudDrive.App.ServicesImpl
{
    internal class SyncSchedulerService : ISyncSchedulerService
    {
        private readonly ILogger<SyncSchedulerService> _logger;
        private readonly ISyncService _syncService;

        private readonly object _queueLock;
        private readonly ConcurrentQueue<Task> _pushQueue;
        private readonly ConcurrentQueue<Task> _pullQueue;
        private bool _isWorkInProgress;

        public SyncSchedulerService(
            ILogger<SyncSchedulerService> logger,
            ISyncService syncService)
        {
            _logger = logger;
            _syncService = syncService;

            _queueLock = new object();
            _pushQueue = new ConcurrentQueue<Task>();
            _pullQueue = new ConcurrentQueue<Task>();
            _isWorkInProgress = false;
        }


        public bool IsBusy => _isWorkInProgress;

        public event EventHandler<bool>? BusyStatusChanged;


        public Task ScheduleSynchronizeAllFiles()
        {
            return SchedulePullAction(async () => await _syncService.SynchronizeAllFilesAsync());
        }

        public Task ScheduleUploadNewFileToRemote(WatchedFileSystemPath path)
        {
            return SchedulePushAction(async () => await _syncService.UploadNewFileToRemoteAsync(path));
        }

        public Task ScheduleUploadModifiedFileToRemote(WatchedFileSystemPath path)
        {
            return SchedulePushAction(async () => await _syncService.UploadModifiedFileToRemoteAsync(path));
        }

        public Task ScheduleUploadRenamedFileToRemote(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath)
        {
            return SchedulePushAction(async () => await _syncService.UploadRenamedFileToRemoteAsync(oldPath, newPath));
        }

        public Task ScheduleRemoveFileFromRemote(WatchedFileSystemPath path)
        {
            return SchedulePushAction(async () => await _syncService.RemoveFileFromRemoteAsync(path));
        }

        public Task ScheduleRestoreFileFromRemote(Guid fileId)
        {
            return SchedulePushAction(async () => await _syncService.RestoreFileFromRemoteAsync(fileId));
        }

        public Task ScheduleRestoreFileFromRemote(Guid fileId, Guid fileVersionId)
        {
            return SchedulePushAction(async () => await _syncService.RestoreFileFromRemoteAsync(fileId, fileVersionId));
        }

        public Task ScheduleUploadNewFolderRecursively(WatchedFileSystemPath folderPath)
        {
            return SchedulePushAction(async () => await _syncService.UploadNewFolderRecursivelyAsync(folderPath));
        }

        public Task ScheduleUploadNewFolderToRemote(WatchedFileSystemPath path)
        {
            return SchedulePushAction(async () => await _syncService.UploadNewFolderToRemoteAsync(path));
        }

        public Task ScheduleUploadModifiedFolderToRemote(WatchedFileSystemPath path)
        {
            return SchedulePushAction(async () => await _syncService.UploadModifiedFolderToRemoteAsync(path));
        }

        public Task ScheduleUploadRenamedFolderToRemote(WatchedFileSystemPath oldPath, WatchedFileSystemPath newPath)
        {
            return SchedulePushAction(async () => await _syncService.UploadRenamedFolderToRemoteAsync(oldPath, newPath));
        }

        public Task ScheduleRemoveFoldersFromRemote(WatchedFileSystemPath path)
        {
            return SchedulePushAction(async () => await _syncService.RemoveFoldersFromRemoteAsync(path));
        }

        public Task ScheduleRestoreFolderFromRemote(Guid fileId)
        {
            return SchedulePushAction(async () => await _syncService.RestoreFolderFromRemoteAsync(fileId));
        }

        public Task ScheduleRestoreFolderFromRemote(Guid fileId, Guid fileVersionId)
        {
            return SchedulePushAction(async () => await _syncService.RestoreFolderFromRemoteAsync(fileId, fileVersionId));
        }



        private Task SchedulePushAction(Action action)
        {
            var task = new Task(action);
            _pushQueue.Enqueue(task);
            StartWorkIfNotBusy();
            return task;
        }

        private Task SchedulePullAction(Action action)
        {
            var task = new Task(action);
            _pullQueue.Enqueue(task);
            StartWorkIfNotBusy();
            return task;
        }

        private void StartWorkIfNotBusy()
        {
            lock (_queueLock)
            {
                if (!_isWorkInProgress && (!_pushQueue.IsEmpty || !_pullQueue.IsEmpty))
                {
                    Task.Run(DoScheduledWork);
                }
            }
        }

        private async void DoScheduledWork()
        {
            _isWorkInProgress = true;
            BusyStatusChanged?.Invoke(this, _isWorkInProgress);

            // execute all pull tasks one by one
            while (_pullQueue.TryDequeue(out var task))
            {
                try
                {
                    task.Start();
                    await task;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas wykonywania zaplanowanej akcji synchronizacji z chmury");
                }
            }

            // execute all push tasks at around the same time
            foreach(var task in _pushQueue)
            {
                task.Start();
            }

            try
            {
                await Task.WhenAll(_pushQueue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wykonywania zaplanowanej akcji synchronizacji do chmury");
            }

            _isWorkInProgress = false;
            BusyStatusChanged?.Invoke(this, _isWorkInProgress);
        }
    }
}
