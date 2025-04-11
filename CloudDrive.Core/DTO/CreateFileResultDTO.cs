using CloudDrive.Core.Domain.Entities;
using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.DTO
{
    public class CreateFileResultDTO
    {
        public required Entities.File FileInfo { get; set; }
        public required FileVersion FirstFileVersionInfo { get; set; }
    }
}
