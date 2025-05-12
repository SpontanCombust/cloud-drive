namespace CloudDrive.Core.DTO
{
    public class CreateDirectoryResultDTO
    {
        public required FileDTO FileInfo { get; set; }
        public required FileVersionDTO FirstFileVersionInfo { get; set; }
    }
}
