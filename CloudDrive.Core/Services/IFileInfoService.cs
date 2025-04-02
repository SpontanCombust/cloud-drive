using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Services
{
    public interface IFileInfoService
    {
        Task<Entities.File> CreateInfoForNewFile(Guid fileId, Guid userId);
        Task<Entities.File?> GetInfoForFile(Guid fileId);
        Task<bool> FileBelongsToUser(Guid fileId, Guid userId);
    }
}
