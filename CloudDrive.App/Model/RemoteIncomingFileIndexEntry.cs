namespace CloudDrive.App.Model
{
    public class RemoteIncomingFileIndexEntry
    {
        public required Guid FileId { get; set; }

        public required bool IsDirectory { get; set; }

        public required Guid FileVersionId { get; set; }

        public required string WatchedFolderPath { get; set; }

        public required string FullPath { get; set; }

        public required int VersionNr { get; set; }

        public required string? Md5 { get; set; }

        public required long? SizeBytes { get; set; }

        public required DateTime VersionCreatedDate { get; set; }



        public WatchedFileSystemPath GetWatchedFileSystemPath()
        {
            return new WatchedFileSystemPath(FullPath, WatchedFolderPath, IsDirectory);
        }
    }
}
