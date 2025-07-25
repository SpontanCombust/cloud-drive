﻿using LiteDB;

namespace CloudDrive.App.Model
{   
    public class LocalCommitedFileIndexEntry
    {
        [BsonId]
        public required Guid FileId { get; set; }

        public required bool IsDirectory { get; set; }

        public required Guid FileVersionId { get; set; }

        public required string WatchedFolderPath { get; set; }

        public required string FullPath { get; set; }

        public required int VersionNr { get; set; }

        public required string? Md5 { get; set; }

        public required long? SizeBytes { get; set; }

        public required DateTime VersionCreatedDate { get; set; }

        public required DateTime LocalCommitedDate { get; set; }


        public static LocalCommitedFileIndexEntry FromFileVersionAndPath(FileVersionDTO fileVersionInfo, WatchedFileSystemPath path)
        {
            return new LocalCommitedFileIndexEntry
            {
                FileId = fileVersionInfo.FileId,
                IsDirectory = path.IsDirectory,
                FileVersionId = fileVersionInfo.FileVersionId,
                WatchedFolderPath = path.WatchedFolder,
                FullPath = path.Full,
                VersionNr = fileVersionInfo.VersionNr,
                Md5 = fileVersionInfo.Md5,
                SizeBytes = fileVersionInfo.SizeBytes,
                VersionCreatedDate = fileVersionInfo.CreatedDate.DateTime,
                LocalCommitedDate = DateTime.UtcNow
            };
        }

        public static LocalCommitedFileIndexEntry FromRemoteIncomingIndexEntryAndPath(RemoteIncomingFileIndexEntry remoteIncoming, WatchedFileSystemPath path)
        {
            return new LocalCommitedFileIndexEntry
            {
                FileId = remoteIncoming.FileId,
                IsDirectory = path.IsDirectory,
                FileVersionId = remoteIncoming.FileVersionId,
                WatchedFolderPath = path.WatchedFolder,
                FullPath = path.Full,
                VersionNr = remoteIncoming.VersionNr,
                Md5 = remoteIncoming.Md5,
                SizeBytes = remoteIncoming.SizeBytes,
                VersionCreatedDate = remoteIncoming.VersionCreatedDate,
                LocalCommitedDate = DateTime.UtcNow
            };
        }

        public static void EnsureIndices(ILiteCollection<LocalCommitedFileIndexEntry> collection)
        {
            ArgumentNullException.ThrowIfNull(collection, nameof(collection));

            collection.EnsureIndex(x => x.FileId, true);
            collection.EnsureIndex(x => x.FileVersionId, true);
            collection.EnsureIndex(x => x.FullPath, true);
        }



        public WatchedFileSystemPath GetWatchedFileSystemPath()
        {
            return new WatchedFileSystemPath(FullPath, WatchedFolderPath, IsDirectory);
        }
    }
}
