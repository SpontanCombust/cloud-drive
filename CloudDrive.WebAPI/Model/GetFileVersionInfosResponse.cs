using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class GetFileVersionInfosResponse
    {
        public required FileVersionDTO[] FileVersionsInfos { get; set; }
    }
}
