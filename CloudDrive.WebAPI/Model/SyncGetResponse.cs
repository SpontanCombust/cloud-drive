using CloudDrive.Infrastructure.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class SyncGetResponse
    {
        public required FileVersionDTO[] CurrentFileVersionsInfos { get; set; }
    }
}
