using CloudDrive.Infrastructure.DTO;

namespace CloudDrive.Infrastructure.Commands
{
    public class SyncGetResponse
    {
        public required FileVersionDTO[] CurrentFileVersionsInfos {  get; set; }
    }
}
