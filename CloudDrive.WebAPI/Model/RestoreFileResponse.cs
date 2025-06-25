using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class RestoreFileResponse
    {
        public required FileDTO FileInfo { get; set; }
        public required FileVersionDTO ActiveFileVersionInfo { get; set; }
        public required DateTime ServerTime { get; set; }
    }
}
