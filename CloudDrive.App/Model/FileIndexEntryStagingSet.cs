namespace CloudDrive.App.Model
{
    public static class FileIndexEntryStagingSet
    {
        public class AddedLocally
        {
            public required LocalIncomingFileIndexEntry LocalIncoming { get; set; }
        }

        public class AddedRemotely
        {
            public required RemoteIncomingFileIndexEntry RemoteIncoming { get; set; }
        }

        public class DeletedLocally
        {
            public required LocalCommitedFileIndexEntry LocalCommited { get; set; }
            public RemoteIncomingFileIndexEntry? RemoteIncoming { get; set; }
        }

        public class DeletedRemotely
        {
            public required LocalCommitedFileIndexEntry LocalCommited { get; set; }
            public LocalIncomingFileIndexEntry? LocalIncoming { get; set; }
        }

        public class ModifedLocally
        {
            public required LocalCommitedFileIndexEntry LocalCommited { get; set; }
            public required LocalIncomingFileIndexEntry LocalIncoming { get; set; }
            public RemoteIncomingFileIndexEntry? RemoteIncoming { get; set; }
        }

        public class ModifiedRemotely
        {
            public required LocalCommitedFileIndexEntry LocalCommited { get; set; }
            public required RemoteIncomingFileIndexEntry RemoteIncoming { get; set; }
            public LocalIncomingFileIndexEntry? LocalIncoming { get; set; }
        }
    }
}
