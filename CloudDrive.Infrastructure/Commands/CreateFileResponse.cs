using CloudDrive.Infrastructure.DTO;

namespace CloudDrive.Infrastructure.Commands
{
    public class CreateFileResponse
    {
        public required FileDTO FileInfo {  get; set; }
        public required FileVersionDTO FirstFileVersionInfo { get; set; }
    }
}
