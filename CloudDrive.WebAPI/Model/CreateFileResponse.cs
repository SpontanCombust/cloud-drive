using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class CreateFileResponse
    {
        public required FileDTO FileInfo {  get; set; }
        public required FileVersionDTO FirstFileVersionInfo { get; set; }
        public required DateTime ServerTime { get; set; }
    }
}
