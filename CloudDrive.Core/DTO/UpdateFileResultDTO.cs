namespace CloudDrive.Core.DTO
{
    public class UpdateFileResultDTO
    {
        public required FileVersionDTO ActiveFileVersion { get; set; }
        public required bool Changed { get; set; }
    }
}
