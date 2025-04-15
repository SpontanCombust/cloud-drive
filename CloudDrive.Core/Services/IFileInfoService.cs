using CloudDrive.Infrastructure.DTO;

namespace CloudDrive.Core.Services
{
    public interface IFileInfoService
    {
        Task<FileDTO> CreateInfoForNewFile(Guid fileId, Guid userId, bool isDir);
        Task<FileDTO?> GetInfoForFile(Guid fileId);
        Task<bool> FileBelongsToUser(Guid fileId, Guid userId);
    }
}
