using CloudDrive.Core.DTO;

namespace CloudDrive.Core.Services
{
    public interface IFileVersionInfoService
    {
        Task<FileVersionDTO> CreateInfoForNewFileVersion(
            Guid fileVersionId,
            Guid fileId, 
            string clientDirPath, 
            string clientFileName,
            string serverDirPath,
            string serverFileName,
            string md5Hash,
            long fileSize
        );
        Task<FileVersionDTO?> GetInfoForFileVersion(Guid fileVersionId);
        Task<FileVersionDTO?> GetInfoForFileVersionByVersionNr(Guid fileId, int versionNr);
        Task<FileVersionDTO?> GetInfoForLatestFileVersion(Guid fileId);
        Task<FileVersionDTO[]> GetInfoForAllLatestUserFileVersions(Guid userId);
        /// <summary>
        /// Find if for a user there ever was a file version with these exact same content characteristics
        /// </summary>
        Task<FileVersionDTO?> GetInfoForUserFileVersionByUniqueContent(Guid userId, string md5Hash, long fileSize);
    }
}
