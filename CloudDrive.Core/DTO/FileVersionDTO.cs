using System.Text.Json.Serialization;

//FIXME wrong namespace
namespace CloudDrive.Infrastructure.DTO
{
    public class FileVersionDTO
    {
        public Guid FileVersionId { get; set; }
        public Guid FileId { get; set; }
        public string ClientDirPath { get; set; }
        public string ClientFileName { get; set; }
        [JsonIgnore] public string ServerDirPath { get; set; }
        [JsonIgnore] public string ServerFileName { get; set; }
        public int VersionNr { get; set; }
        public string Md5 { get; set; }
        public long SizeByes { get; set; } //FIXME typo
        public DateTime CreatedDate { get; set; }
    }
}
