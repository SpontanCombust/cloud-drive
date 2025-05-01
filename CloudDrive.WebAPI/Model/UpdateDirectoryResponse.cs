using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class UpdateDirectoryResponse
    {
        public required FileVersionDTO NewFileVersionInfo { get; set; }
    }
}
