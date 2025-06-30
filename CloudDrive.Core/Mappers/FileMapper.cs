using CloudDrive.Core.DTO;
using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Mappers
{
    public static class FileMapper
    {
        public static FileDTO ToDto(this Entities.File file)
        {
            return new FileDTO
            {
                FileId = file.FileId,
                UserId = file.UserId,
                IsDir = file.IsDir,
                Deleted = file.Deleted,
                ActiveFileVersionId = file.ActiveFileVersionId,
                CreatedDate = file.CreatedDate,
                ModifiedDate = file.ModifiedDate,
            };
        }
    }
}
