using CloudDrive.App.Model;

namespace CloudDrive.App.Utils
{
    /// <summary>
    /// Class designed to deduce the changes in the file index between the local and remote state.
    /// </summary>
    public class FileIndexStagingHelper
    {
        private HashSet<WatchedFileSystemPath> _localCommitedPaths;
        private HashSet<WatchedFileSystemPath> _localIncomingPaths;
        private HashSet<WatchedFileSystemPath> _remoteIncomingPaths;

        private Dictionary<WatchedFileSystemPath, LocalCommitedFileIndexEntry> _localCommitedDict;
        private Dictionary<WatchedFileSystemPath, LocalIncomingFileIndexEntry> _localIncomingDict;
        private Dictionary<WatchedFileSystemPath, RemoteIncomingFileIndexEntry> _remoteIncomingDict;

        public FileIndexStagingHelper()
        {
            _localCommitedPaths = new();
            _localIncomingPaths = new();
            _remoteIncomingPaths = new();

            _localCommitedDict = new();
            _localIncomingDict = new();
            _remoteIncomingDict = new();
        }

        public void Stage(
            IEnumerable<LocalCommitedFileIndexEntry> localCommitedEntries,
            IEnumerable<LocalIncomingFileIndexEntry> localIncomingEntries,
            IEnumerable<RemoteIncomingFileIndexEntry> remoteIncomingEntries)
        {
            _localCommitedDict = localCommitedEntries.ToDictionary(
                e => new WatchedFileSystemPath(e.FullPath, e.WatchedFolderPath, e.IsDirectory), 
                e => e);
            _localIncomingDict = localIncomingEntries.ToDictionary(
                e => new WatchedFileSystemPath(e.FullPath, e.WatchedFolderPath, e.IsDirectory), 
                e => e);
            _remoteIncomingDict = remoteIncomingEntries.ToDictionary(
                e => new WatchedFileSystemPath(e.FullPath, e.WatchedFolderPath, e.IsDirectory), 
                e => e);

            _localCommitedPaths = _localCommitedDict.Keys.ToHashSet();
            _localIncomingPaths = _localIncomingDict.Keys.ToHashSet();
            _remoteIncomingPaths = _remoteIncomingDict.Keys.ToHashSet();
        }

        public IEnumerable<FileIndexEntryStagingSet.AddedLocally> AddedLocally()
        {
            var paths = _localIncomingPaths.Except(_localCommitedPaths);//.Except(_remoteIncomingPaths);

            foreach (var path in paths)
            {
                if (_localIncomingDict.TryGetValue(path, out var localIncomingEntry))
                {
                    yield return new FileIndexEntryStagingSet.AddedLocally
                    {
                        LocalIncoming = localIncomingEntry
                    };
                }
            }
        }

        public IEnumerable<FileIndexEntryStagingSet.AddedRemotely> AddedRemotely()
        {
            var paths = _remoteIncomingPaths.Except(_localCommitedPaths);//.Except(_localIncomingPaths);

            foreach (var path in paths)
            {
                if (_remoteIncomingDict.TryGetValue(path, out var remoteIncomingEntry))
                {
                    yield return new FileIndexEntryStagingSet.AddedRemotely
                    {
                        RemoteIncoming = remoteIncomingEntry
                    };
                }
            }
        }

        public IEnumerable<FileIndexEntryStagingSet.DeletedLocally> DeletedLocally()
        {
            var paths = _localCommitedPaths.Except(_localIncomingPaths);//.Intersect(_remoteIncomingPaths);

            foreach (var path in paths)
            {
                if (_localCommitedDict.TryGetValue(path, out var localCommitedEntry))
                {
                    _remoteIncomingDict.TryGetValue(path, out var remoteIncomingEntry);

                    yield return new FileIndexEntryStagingSet.DeletedLocally
                    {
                        LocalCommited = localCommitedEntry,
                        RemoteIncoming = remoteIncomingEntry
                    };
                }
            }
        }

        public IEnumerable<FileIndexEntryStagingSet.DeletedRemotely> DeletedRemotely()
        {
            var paths = _localCommitedPaths.Except(_remoteIncomingPaths);//.Intersect(_localIncomingPaths);

            foreach (var path in paths)
            {
                if (_localCommitedDict.TryGetValue(path, out var localCommitedEntry))
                {
                    _localIncomingDict.TryGetValue(path, out var localIncomingEntry);

                    yield return new FileIndexEntryStagingSet.DeletedRemotely
                    {
                        LocalCommited = localCommitedEntry,
                        LocalIncoming = localIncomingEntry
                    };
                }
            }
        }

        public IEnumerable<FileIndexEntryStagingSet.ModifedLocally> ModifiedLocally()
        {
            var paths = _localCommitedPaths.Intersect(_localIncomingPaths);//.Intersect(_remoteIncomingPaths);

            foreach (var path in paths)
            {
                if (_localCommitedDict.TryGetValue(path, out var localCommitedEntry) &&
                    _localIncomingDict.TryGetValue(path, out var localIncomingEntry) &&
                    localIncomingEntry.LastModifiedDate > localCommitedEntry.CommitedDate)
                {
                    _remoteIncomingDict.TryGetValue(path, out var remoteIncomingEntry);

                    yield return new FileIndexEntryStagingSet.ModifedLocally
                    {
                        LocalCommited = localCommitedEntry,
                        LocalIncoming = localIncomingEntry,
                        RemoteIncoming = remoteIncomingEntry
                    };
                }
            }
        }

        public IEnumerable<FileIndexEntryStagingSet.ModifiedRemotely> ModifiedRemotely()
        {
            var paths = _localCommitedPaths.Intersect(_remoteIncomingPaths);//.Intersect(_localIncomingPaths);

            foreach (var path in paths)
            {
                if (_localCommitedDict.TryGetValue(path, out var localCommitedEntry) &&
                    _remoteIncomingDict.TryGetValue(path, out var remoteIncomingEntry) &&
                    remoteIncomingEntry.VersionCreatedDate > localCommitedEntry.CommitedDate)
                {
                    _localIncomingDict.TryGetValue(path, out var localIncomingEntry);

                    yield return new FileIndexEntryStagingSet.ModifiedRemotely
                    {
                        LocalCommited = localCommitedEntry,
                        RemoteIncoming = remoteIncomingEntry,
                        LocalIncoming = localIncomingEntry
                    };
                }
            }
        }
    }
}
