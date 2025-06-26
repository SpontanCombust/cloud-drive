namespace CloudDrive.App.Model
{
    public class LocalIncomingFileIndexEntry
    {
        public required string FullPath { get; set; }

        public required string WatchedFolderPath { get; set; }

        public required bool IsDirectory { get; set; }

        public required DateTime LastModifiedDate { get; set; }



        public WatchedFileSystemPath GetWatchedFileSystemPath()
        {
            return new WatchedFileSystemPath(FullPath, WatchedFolderPath, IsDirectory);
        }
    }
}
