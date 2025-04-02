using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Services
{
    public interface IFileVersionInfoService
    {
        Task<Entities.FileVersion> CreateInfoForNewFileVersion(
            Guid fileVersionId,
            Guid fileId, 
            string clientDirPath, 
            string clientFileName,
            string serverDirPath,
            string serverFileName,
            string md5Hash,
            long fileSize
        );
        Task<Entities.FileVersion?> GetInfoForFileVersion(Guid fileVersionId);
        Task<Entities.FileVersion?> GetInfoForFileVersionByVersionNr(Guid fileId, int versionNr);
        Task<Entities.FileVersion?> GetInfoForLatestFileVersion(Guid fileId);
        Task<Entities.FileVersion[]> GetInfoForAllLatestUserFileVersions(Guid userId);
    }
}
