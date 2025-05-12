using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class SyncFileResponse
    {
        public required FileDTO FileInfo { get; set; }
        public required FileVersionDTO CurrentFileVersionInfo { get; set; }
    }
}
