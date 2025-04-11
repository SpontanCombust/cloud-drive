using CloudDrive.Infrastructure.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class CreateFileResponse
    {
        public required FileDTO FileInfo {  get; set; }
        public required FileVersionDTO FirstFileVersionInfo { get; set; }
    }
}
