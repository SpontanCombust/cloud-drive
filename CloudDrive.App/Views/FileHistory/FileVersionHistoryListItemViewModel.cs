namespace CloudDrive.App.Views.FileHistory
{
    internal class FileVersionHistoryListItemViewModel
    {
        public required Guid FileVersionId { get; set; }
        public required string FileName { get; set; }
        public required DateTime CreatedDate { get; set; }
        public required int VersionNr { get; set; }
        public required long SizeBytes { get; set; }
        public required string Md5Hash { get; set; }
    }
}
