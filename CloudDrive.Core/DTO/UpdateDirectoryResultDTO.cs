namespace CloudDrive.Core.DTO
{
    public class UpdateDirectoryResultDTO
    {
        public required FileVersionDTO ActiveFileVersion { get; set; }
        public required bool Changed { get; set; }
    }
}
