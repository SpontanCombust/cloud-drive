using CloudDrive.Core.DTO;

namespace CloudDrive.Core.Services
{
    public interface IFileManagerService
    {
        Task<CreateFileResultDTO> CreateFile(Guid userId, Stream? inputStream, string fileName, string? clientDirPath, bool isDir);
        Task<GetFileResultDTO?> GetFileVersion(Guid fileId, int versionNr);
        Task<GetFileResultDTO?> GetLatestFileVersion(Guid fileId);
        Task<FileVersionDTO> UpdateFile(Guid fileId, Stream? fileStream, string clientFileName, string? clientDirPath);
        Task DeleteFile(Guid fileId);
        Task RestoreFile(Guid fileId);
    }
}
