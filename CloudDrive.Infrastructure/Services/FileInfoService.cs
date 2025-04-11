using CloudDrive.Core.Mappers;
using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.DTO;
using CloudDrive.Infrastructure.Repositories;
using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Infrastructure.Services
{
    public class FileInfoService : IFileInfoService
    {
        private readonly AppDbContext dbContext;

        public FileInfoService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }


        public async Task<FileDTO> CreateInfoForNewFile(Guid fileId, Guid userId)
        {
            var fileInfo = new Entities.File
            {
                FileId = fileId,
                UserId = userId,
                Deleted = false,
            };

            var tracked = (await dbContext.Files.AddAsync(fileInfo)).Entity;
            await dbContext.SaveChangesAsync();

            return tracked.ToDto();
        }

        public async Task<FileDTO?> GetInfoForFile(Guid fileId)
        {
            var file = await dbContext.Files.FindAsync(fileId);
            return file?.ToDto();
        }

        public async Task<bool> FileBelongsToUser(Guid fileId, Guid userId)
        {
            var info = await GetInfoForFile(fileId);
            return info?.UserId == userId;
        }
    }
}
