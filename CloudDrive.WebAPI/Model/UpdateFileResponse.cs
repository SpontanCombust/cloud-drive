using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class UpdateFileResponse
    {
        public required FileVersionDTO NewFileVersionInfo { get; set; }
        public required bool Changed { get; set; }
        public required DateTime ServerTime { get; set; }
    }
}
