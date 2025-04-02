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
                CreatedDate = DateTime.Now.ToUniversalTime()
            };

            var tracked = (await dbContext.FileVersions.AddAsync(info)).Entity;
            await dbContext.SaveChangesAsync();

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
            var activeUserFiles = dbContext.Files
                .Where(f => f.UserId == userId && !f.Deleted);

            var fileAndMaxVersionNr = dbContext.FileVersions
                .GroupBy(fv => fv.FileId)
                .Select(g => new
                {
                    FileId = g.Key,
                    MaxVersionNr = g.Max(fv => fv.VersionNr)
                });

            return await dbContext.FileVersions
                .Join(
                    activeUserFiles,
                    fv => fv.FileId,
                    f => f.FileId,
                    (fv, f) => fv)
                .Join(fileAndMaxVersionNr,
                    fv => new { FileId = fv.FileId, VersionNr = fv.VersionNr },
                    f => new { FileId = f.FileId, VersionNr = f.MaxVersionNr },
                    (fv, _) => fv)
                .ToArrayAsync();
        }
    }
}
