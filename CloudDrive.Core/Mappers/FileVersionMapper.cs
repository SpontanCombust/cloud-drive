using CloudDrive.Core.DTO;
using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Mappers
{
    public static class FileVersionMapper
    {
        public static FileVersionDTO ToDto(this Entities.FileVersion fileVersion)
        {
            return new FileVersionDTO
            {
                FileVersionId = fileVersion.FileVersionId,
                FileId = fileVersion.FileId,
                ClientDirPath = fileVersion.ClientDirPath,
                ClientFileName = fileVersion.ClientFileName,
                ServerDirPath = fileVersion.ServerDirPath,
                ServerFileName = fileVersion.ServerFileName,
                VersionNr = fileVersion.VersionNr,
                CreatedDate = fileVersion.CreatedDate,
                Md5 = fileVersion.Md5,
                SizeBytes = fileVersion.SizeBytes,
            };
        }
    }
}
