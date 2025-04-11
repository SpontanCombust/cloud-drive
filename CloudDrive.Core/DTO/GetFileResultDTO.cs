namespace CloudDrive.Core.DTO
{
    public class GetFileResultDTO
    {
        public byte[] FileContent { get; set; }
        public string ClientFileName { get; set; }
        public string ClientDirPath { get; set; }
    }
}
