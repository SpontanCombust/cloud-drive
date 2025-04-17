using CloudDrive.Infrastructure.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class UpdateFileResponse
    {
        public required FileVersionDTO NewFileVersionInfo { get; set; }
    }
}
