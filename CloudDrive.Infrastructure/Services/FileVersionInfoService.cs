using CloudDrive.Core.Domain.Entities;
using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CloudDrive.Infrastructure.Services
{
    public class FileVersionInfoService : IFileVersionInfoService
    {
        private readonly AppDbContext dbContext;

        public FileVersionInfoService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }


        public async Task<FileVersion> CreateInfoForNewFileVersion(
            Guid fileVersionId, 
            Guid fileId, 
            string clientDirPath, 
            string clientFileName, 
            string serverDirPath, 
            string serverFileName, 
            string md5Hash, 
            long fileSize)
        {
            var info = new FileVersion
            {
                FileVersionId = fileVersionId,
                FileId = fileId,
                ClientDirPath = clientDirPath,
                ClientFileName = clientFileName,
                ServerDirPath = serverDirPath,
                ServerFileName = serverFileName,
                VersionNr = 0,
                Md5 = md5Hash,
                SizeByes = fileSize,
                CreatedDate = DateTime.Now
            };

            var tracked = (await dbContext.FileVersions.AddAsync(info)).Entity;
            return tracked;
        }

        public async Task<FileVersion?> GetInfoForFileVersion(Guid fileVersionId)
        {
            return await dbContext.FileVersions
                .FindAsync(fileVersionId);
        }

        public async Task<FileVersion?> GetInfoForFileVersionByVersionNr(Guid fileId, int versionNr)
        {
            return await dbContext.FileVersions
                .SingleOrDefaultAsync(fv => fv.FileId == fileId && fv.VersionNr == versionNr);
        }

        public async Task<FileVersion?> GetInfoForLatestFileVersion(Guid fileId)
        {
            return await dbContext.FileVersions
                .Where(fv => fv.FileId == fileId)
                .OrderByDescending(fv => fv.VersionNr)
                .FirstOrDefaultAsync();
        }

        public async Task<FileVersion[]> GetInfoForAllLatestUserFileVersions(Guid userId)
        {
            return await dbContext.Files
                .Where(f => f.UserId == userId && !f.Deleted)
                .GroupJoin(
                    dbContext.FileVersions
                        .GroupBy(fv => fv.FileId)
                        .Select(g => g.OrderByDescending(fv => fv.VersionNr).First()),
                    f => f.FileId,
                    fv => fv.FileId,
                    (f, latestFv) => latestFv
                ).SelectMany(fv => fv)
                .ToArrayAsync();
        }
    }
}
