namespace CloudDrive.Infrastructure.DTO
{
    public class FileVersionDTO
    {
        public Guid FileVersionId { get; set; }
        public Guid FileId { get; set; }
        public string ClientDirPath { get; set; }
        public string ClientFileName { get; set; }
        public int VersionNr { get; set; }
        public string Md5 { get; set; }
        public long SizeByes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
