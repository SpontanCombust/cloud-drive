namespace CloudDrive.Core.Domain.Entities
{
    public class FileVersion
    {
        public Guid FileVersionId { get; set; }
        public Guid FileId {  get; set; }
        public string ClientDirPath {  get; set; }
        public string ClientFileName { get; set; }
        public string ServerDirPath { get; set; }
        public string ServerFileName { get; set; }
        public int VersionNr {  get; set; }
        public string Md5 { get; set; }
        public long SizeByes { get; set; }
        public DateTime CreatedDate { get; set; }

        public File File { get; set; }
    }
}
