using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class UpdateDirectoryResponse
    {
        public required FileVersionDTO NewFileVersionInfo { get; set; }
        public required FileVersionExtDTO[] NewSubfileVersionInfosExt { get; set; }
        public required bool Changed { get; set; }
        public required DateTime ServerTime { get; set; }
    }
}
