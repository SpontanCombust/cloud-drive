using CloudDrive.Infrastructure.DTO;

namespace CloudDrive.WebAPI.Model
{
    //TODO rename to SyncAllResponse
    public class SyncGetResponse
    {
        public required FileVersionDTO[] CurrentFileVersionsInfos { get; set; }
    }
}
