namespace CloudDrive.App.Views.FileHistory
{
    public class FileVersionListItemViewModel
    {
        public required Guid FileVersionId { get; set; }
        public required string ClientPath { get; set; }
        public required int VersionNr { get; set; }
        public string? Md5 { get; set; }
        public long? SizeBytes { get; set; }
        public required DateTime CreatedDate { get; set; }
        public required bool Active { get; set; }
    }
}
