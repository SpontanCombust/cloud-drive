using CloudDrive.App.Model;
using CloudDrive.App.Services;
using LiteDB;
using LiteDB.Engine;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace CloudDrive.App.ServicesImpl
{
    internal class LiteDBLocalCommitedFileIndexService : ILocalCommitedFileIndexService, IDisposable
    {
        private readonly ILogger<LiteDBLocalCommitedFileIndexService> _logger;
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<LocalCommitedFileIndexEntry> _index;

        public LiteDBLocalCommitedFileIndexService(
            ILogger<LiteDBLocalCommitedFileIndexService> logger)
        {
            _logger = logger;

            _db = new LiteDatabase(DbFilePath());
            _db.Pragma("UTC_DATE", true);
            _index = _db.GetCollection<LocalCommitedFileIndexEntry>("commited_file_index");
            LocalCommitedFileIndexEntry.EnsureIndices(_index);
        }



        public LocalCommitedFileIndexEntry? Insert(LocalCommitedFileIndexEntry entry)
        {
            var prevEntry = _index.FindById(entry.FileId);
            _index.Upsert(entry);
            return prevEntry;
        }



        public LocalCommitedFileIndexEntry? Find(Guid fileId)
        {
            return _index.FindById(fileId);
        }

        public LocalCommitedFileIndexEntry? FindByWatchedPath(WatchedFileSystemPath path)
        {
            return _index.FindOne(idx =>
                idx.FullPath.Equals(path.Full, StringComparison.OrdinalIgnoreCase)
                && idx.WatchedFolderPath == path.WatchedFolder
                && idx.IsDirectory == path.IsDirectory
            );
        }

        public LocalCommitedFileIndexEntry? FindByRawFullPath(string fullPath)
        {
            return _index.FindOne(idx =>
                idx.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<LocalCommitedFileIndexEntry> FindInDirectory(Guid directoryFileId)
        {
            var directoryEntry = _index.FindById(directoryFileId);
            if (directoryEntry == null)
            {
                throw new ArgumentException("Indeks dla katalogu nie istnieje");
            }
            else if (!directoryEntry.IsDirectory)
            {
                throw new ArgumentException("Indeks nie odpowiada katalogowi");
            }

            string pathPrefix = directoryEntry.FullPath + Path.DirectorySeparatorChar;

            return _index.Find(idx =>
                idx.FullPath.StartsWith(pathPrefix)
            );
        }

        public IEnumerable<LocalCommitedFileIndexEntry> FindAll()
        {
            return _index.FindAll();
        }



        public bool Remove(Guid fileId)
        {
            return _index.Delete(fileId);
        }

        public bool RemoveByPath(WatchedFileSystemPath path)
        {
            var entry = FindByWatchedPath(path);
            if (entry != null)
            {
                return _index.Delete(entry.FileId);
            }
            else
            {
                return false;
            }
        }



        public void Dispose()
        {
            _db.Dispose();
        }



        private static string DbFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cloudDriveDataPath = Path.Combine(appDataPath, "CloudDrive");
            Directory.CreateDirectory(cloudDriveDataPath);
            return Path.Combine(cloudDriveDataPath, $"global.db");
        }     
    }
}
