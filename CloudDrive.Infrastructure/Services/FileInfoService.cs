using CloudDrive.Core.DTO;
using CloudDrive.Core.Mappers;
using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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


        public async Task<FileDTO> CreateInfoForNewFile(Guid fileId, Guid userId, bool isDir, Guid firstFileVersionId)
        {
            var fileInfo = new Entities.File
            {
                FileId = fileId,
                UserId = userId,
                IsDir = isDir,
                Deleted = false,
                ActiveFileVersionId = firstFileVersionId,
                CreatedDate = DateTime.Now.ToUniversalTime(),
                ModifiedDate = null
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

        public async Task<FileDTO[]> GetInfoForManyFiles(Guid[] fileIds)
        {
            var files = await dbContext.Files.Where(f => fileIds.Contains(f.FileId)).ToArrayAsync();
            return files.Select(f => f.ToDto()).ToArray();
        }

        public async Task<bool> FileBelongsToUser(Guid fileId, Guid userId)
        {
            var info = await dbContext.Files.FindAsync(fileId);
            return info?.UserId == userId;
        }

        public async Task<bool> FileIsDirectory(Guid fileId)
        {
            var info = await dbContext.Files.FindAsync(fileId);
            return info?.IsDir == true;
        }

        public async Task<bool> FileIsRegularFile(Guid fileId)
        {
            var info = await dbContext.Files.FindAsync(fileId);
            return info?.IsDir == false;
        }

        public async Task<FileDTO> UpdateInfoForFile(Guid fileId, bool? deleted, Guid? activeFileVersionId)
        {
            //TODO add custom standard exception types
            var tracked = await dbContext.Files.FindAsync(fileId) ?? throw new Exception("File not found");

            tracked.Deleted = deleted ?? tracked.Deleted;
            tracked.ActiveFileVersionId = activeFileVersionId ?? tracked.ActiveFileVersionId;
            tracked.ModifiedDate = DateTime.Now.ToUniversalTime();
            await dbContext.SaveChangesAsync();

            return tracked.ToDto();
        }

        public async Task UpdateInfoForManyFiles(FileDTO[] filesToUpdate)
        {
            foreach (var fileDto in filesToUpdate)
            {
                var tracked = await dbContext.Files.FindAsync(fileDto.FileId);
                if (tracked != null)
                {
                    tracked.Deleted = fileDto.Deleted;
                    tracked.ActiveFileVersionId = fileDto.ActiveFileVersionId;
                    tracked.ModifiedDate = DateTime.Now.ToUniversalTime();
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
