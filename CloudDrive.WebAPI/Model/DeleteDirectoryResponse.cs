using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class DeleteDirectoryResponse
    {
        public required FileDTO[] AffectedSubfiles { get; set; }
        public required FileVersionDTO[] AffectedSubfileVersions { get; set; }
        public required DateTime ServerTime { get; set; }
    }
}
