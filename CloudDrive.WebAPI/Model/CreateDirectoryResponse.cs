using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class CreateDirectoryResponse
    {
        public required FileDTO FileInfo {  get; set; }
        public required FileVersionDTO FirstFileVersionInfo { get; set; }
    }
}
