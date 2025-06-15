using CloudDrive.Core.DTO;

namespace CloudDrive.Core.Services
{
    public interface IFileInfoService
    {
        Task<FileDTO> CreateInfoForNewFile(Guid fileId, Guid userId, bool isDir, Guid firstFileVersionId);
        Task<FileDTO?> GetInfoForFile(Guid fileId);
        Task<FileDTO[]> GetInfoForManyFiles(Guid[] fileIds);
        Task<bool> FileBelongsToUser(Guid fileId, Guid userId);
        Task<bool> FileIsDirectory(Guid fileId);
        Task<bool> FileIsRegularFile(Guid fileId);
        // Pass non-null values if you want to update them
        Task<FileDTO> UpdateInfoForFile(Guid fileId, bool? deleted, Guid? activeFileVersionId);
        Task UpdateInfoForManyFiles(FileDTO[] filesToUpdate);
    }
}
