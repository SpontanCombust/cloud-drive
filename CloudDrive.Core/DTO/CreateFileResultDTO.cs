namespace CloudDrive.Core.DTO
{
    public class CreateFileResultDTO
    {
        public required FileDTO FileInfo { get; set; }
        public required FileVersionDTO FirstFileVersionInfo { get; set; }
    }
}
