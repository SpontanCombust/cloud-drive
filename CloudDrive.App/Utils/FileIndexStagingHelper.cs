using CloudDrive.App.Model;

namespace CloudDrive.App.Utils
{
    /// <summary>
    /// Klasa stworzona do wykrywania zmian w indeksie plików pomiędzy trzema stanami: lokalnym utwalonym, lokalnym "na żywo" i obecnym zdalnym.
    /// Istnieje dla potrzeb pełnej synchronizacji i pobierania zmian z serwera.
    /// Nie jest w stanie wykryć złożonych zmian lokalnych typu przeniesienie pliku, jeśli ta operacja została wykonana poza czasem pracy serwisu FileSystemSyncWatcher.
    /// </summary>
    public class FileIndexStagingHelper
    {
        // Lokalne operacje dodawania i usuwania plików wykrywane są na podstawie istnienia ich ścieżek w indeksie plików...
        private Dictionary<WatchedFileSystemPath, LocalCommitedFileIndexEntry> _localCommitedPathDict = new();
        private Dictionary<WatchedFileSystemPath, LocalIncomingFileIndexEntry> _localIncomingPathDict = new();
        private HashSet<WatchedFileSystemPath> _localCommitedPaths = new();
        private HashSet<WatchedFileSystemPath> _localIncomingPaths = new();

        // Operacje na plikach, które zostały już wysłane na serwer, zatwierdzone i przypisane zostało im ID są wykrywane natomiast na podstawie ID plików w indeksie plików.
        // Jest tak dlatego, że zmiana ścieżki pliku nie wpływa na jego ID, ale jak najbardziej wpływa na jego ścieżkę w indeksie plików.
        private Dictionary<Guid, LocalCommitedFileIndexEntry> _localCommitedIdDict = new();
        private Dictionary<Guid, RemoteIncomingFileIndexEntry> _remoteIncomingIdDict = new();
        private HashSet<Guid> _localCommitedIds = new();
        private HashSet<Guid> _remoteIncomingIds = new();


        public FileIndexStagingHelper()
        {
            
        }

        public void Stage(
            IEnumerable<LocalCommitedFileIndexEntry> localCommitedEntries,
            IEnumerable<LocalIncomingFileIndexEntry> localIncomingEntries,
            IEnumerable<RemoteIncomingFileIndexEntry> remoteIncomingEntries)
        {
            _localCommitedPathDict = localCommitedEntries.ToDictionary(
                e => new WatchedFileSystemPath(e.FullPath, e.WatchedFolderPath, e.IsDirectory), 
                e => e);
            _localIncomingPathDict = localIncomingEntries.ToDictionary(
                e => new WatchedFileSystemPath(e.FullPath, e.WatchedFolderPath, e.IsDirectory), 
                e => e);

            _localCommitedPaths = _localCommitedPathDict.Keys.ToHashSet();
            _localIncomingPaths = _localIncomingPathDict.Keys.ToHashSet();


            _localCommitedIdDict = localCommitedEntries.ToDictionary(
                e => e.FileId, 
                e => e);
            _remoteIncomingIdDict = remoteIncomingEntries.ToDictionary(
                e => e.FileId, 
                e => e);

            _localCommitedIds = _localCommitedIdDict.Keys.ToHashSet();
            _remoteIncomingIds = _remoteIncomingIdDict.Keys.ToHashSet();
        }

        public IEnumerable<FileIndexEntryStagingSet.AddedLocally> AddedLocally()
        {
            // tutaj nowy plik mógłby być tak na prawdę plikiem przeniesionym z innej lokalizacji, ale nie mamy informacji o tym, że był przeniesiony
            // pozyskanie tej informacji wymaga analizy heurystycznej systemu plików
            var paths = _localIncomingPaths.Except(_localCommitedPaths);

            foreach (var path in paths)
            {
                if (_localIncomingPathDict.TryGetValue(path, out var localIncomingEntry))
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
            var fileIds = _remoteIncomingIds.Except(_localCommitedIds);

            foreach (var fileId in fileIds)
            {
                if (_remoteIncomingIdDict.TryGetValue(fileId, out var remoteIncomingEntry))
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
            // tutaj usunięcie pliku mógłoby być tak na prawdę efektem przeniesienia pliku wciąż istniejącego, ale nie mamy informacji o tym, że był przeniesiony
            // pozyskanie tej informacji wymaga analizy heurystycznej systemu plików
            var paths = _localCommitedPaths.Except(_localIncomingPaths);

            foreach (var path in paths)
            {
                if (_localCommitedPathDict.TryGetValue(path, out var localCommitedEntry))
                {
                    _remoteIncomingIdDict.TryGetValue(localCommitedEntry.FileId, out var remoteIncomingEntry);

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
            var fileIds = _localCommitedIds.Except(_remoteIncomingIds);

            foreach (var fileId in fileIds)
            {
                if (_localCommitedIdDict.TryGetValue(fileId, out var localCommitedEntry))
                {
                    _localIncomingPathDict.TryGetValue(localCommitedEntry.GetWatchedFileSystemPath(), out var localIncomingEntry);

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
            // tutaj może być wykryta tylko zmiana zawartości pliku
            // wykrycie przeniesienia pliku wymaga bardziej złożonej analizy heurystycznej systemu plików
            var paths = _localCommitedPaths.Intersect(_localIncomingPaths);

            foreach (var path in paths)
            {
                if (_localCommitedPathDict.TryGetValue(path, out var localCommitedEntry) &&
                    _localIncomingPathDict.TryGetValue(path, out var localIncomingEntry) &&
                    localIncomingEntry.LastModifiedDate > localCommitedEntry.LocalCommitedDate)
                {
                    _remoteIncomingIdDict.TryGetValue(localCommitedEntry.FileId, out var remoteIncomingEntry);

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
            var fileIds = _localCommitedIds.Intersect(_remoteIncomingIds);

            foreach (var fileId in fileIds)
            {
                if (_localCommitedIdDict.TryGetValue(fileId, out var localCommitedEntry) &&
                    _remoteIncomingIdDict.TryGetValue(fileId, out var remoteIncomingEntry) &&
                    remoteIncomingEntry.FileModifiedDate > localCommitedEntry.LocalCommitedDate)
                {
                    _localIncomingPathDict.TryGetValue(localCommitedEntry.GetWatchedFileSystemPath(), out var localIncomingEntry);

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
