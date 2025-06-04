using CloudDrive.Core.DTO;

namespace CloudDrive.Core.Services
{
    public interface IFileManagerService
    {
        Task<CreateFileResultDTO> CreateFile(Guid userId, Stream inputStream, string fileName, string? clientDirPath);
        Task<GetFileResultDTO?> GetFileVersion(Guid fileId, int versionNr);
        [Obsolete("Use GetActiveFileVersion instead")]
        Task<GetFileResultDTO?> GetLatestFileVersion(Guid fileId);
        Task<GetFileResultDTO?> GetActiveFileVersion(Guid fileId);
        Task<FileVersionDTO> UpdateFile(Guid fileId, Stream fileStream, string clientFileName, string? clientDirPath);
        Task DeleteFile(Guid fileId);
        Task RestoreFile(Guid fileId);

        Task<CreateDirectoryResultDTO> CreateDirectory(Guid userId, string fileName, string? clientDirPath);
        Task<FileVersionDTO> UpdateDirectory(Guid fileId, string clientFileName, string? clientDirPath);
        Task DeleteDirectory(Guid fileId);
        Task RestoreDirectory(Guid fileId);
    }
}
