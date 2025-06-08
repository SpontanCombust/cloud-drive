using CloudDrive.Core.DTO;

namespace CloudDrive.Core.Services
{
    public interface IFileVersionInfoService
    {
        Task<FileVersionDTO> CreateInfoForNewFileVersion(
            Guid fileVersionId,
            Guid fileId, 
            string? clientDirPath, 
            string clientFileName,
            string? serverDirPath,
            string? serverFileName,
            string? md5Hash,
            long? fileSize
        );
        Task<FileVersionDTO?> GetInfoForFileVersion(Guid fileVersionId);
        Task<FileVersionDTO?> GetInfoForFileVersionByVersionNr(Guid fileId, int versionNr);
        [Obsolete("Use GetInfoForActiveFileVersion instead")]
        Task<FileVersionDTO?> GetInfoForLatestFileVersion(Guid fileId);
        [Obsolete("Use GetInfoForAllActiveUserFileVersions instead")]
        Task<FileVersionDTO[]> GetInfoForAllLatestUserFileVersions(Guid userId);
        Task<FileVersionDTO?> GetInfoForActiveFileVersion(Guid fileId);
        Task<FileVersionDTO[]> GetInfoForUserFileVersions(Guid userId, Guid fileId);
        Task<FileVersionDTO[]> GetInfoForAllActiveUserFileVersions(Guid userId);
        Task<FileVersionExtDTO[]> GetInfoForAllActiveUserFileVersionsExt(Guid userId, bool includeDeleted);
        /// <summary>
        /// Find if for a user there ever was a file version with these exact same content characteristics
        /// </summary>
        Task<FileVersionDTO?> GetInfoForUserFileVersionByUniqueContent(Guid userId, string md5Hash, long fileSize);
    }
}
