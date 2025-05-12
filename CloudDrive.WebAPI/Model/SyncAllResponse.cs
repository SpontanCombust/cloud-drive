using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class SyncAllResponse
    {
        public required FileVersionDTO[] CurrentFileVersionsInfos { get; set; }
    }
}
