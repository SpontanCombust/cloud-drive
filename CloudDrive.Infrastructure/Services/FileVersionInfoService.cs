using CloudDrive.Core.Domain.Entities;
using CloudDrive.Core.DTO;
using CloudDrive.Core.Mappers;
using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Linq;

namespace CloudDrive.Infrastructure.Services
{
    public class FileVersionInfoService : IFileVersionInfoService
    {
        private readonly AppDbContext dbContext;

        public FileVersionInfoService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }


        public async Task<FileVersionDTO> CreateInfoForNewFileVersion(
            Guid fileVersionId, 
            Guid fileId, 
            string? clientDirPath, 
            string clientFileName, 
            string? serverDirPath, 
            string? serverFileName, 
            string? md5Hash, 
            long? fileSize)
        {
            int newVersionNr = await dbContext.FileVersions
                .Where(fv => fv.FileId == fileId)
                .OrderByDescending(fv => fv.VersionNr)
                .Select(fv => fv.VersionNr + 1)
                .FirstOrDefaultAsync();

            var info = new FileVersion
            {
                FileVersionId = fileVersionId,
                FileId = fileId,
                ClientDirPath = clientDirPath,
                ClientFileName = clientFileName,
                ServerDirPath = serverDirPath,
                ServerFileName = serverFileName,
                VersionNr = newVersionNr,
                Md5 = md5Hash,
                SizeBytes = fileSize,
                CreatedDate = DateTime.Now.ToUniversalTime()
            };

            var tracked = (await dbContext.FileVersions.AddAsync(info)).Entity;
            await dbContext.SaveChangesAsync();

            return tracked.ToDto();
        }

        public async Task<FileVersionDTO?> GetInfoForFileVersion(Guid fileVersionId)
        {
            var info = await dbContext.FileVersions.FindAsync(fileVersionId);
            return info?.ToDto();
        }

        public async Task<FileVersionDTO?> GetInfoForUserFileVersion(Guid userId, Guid fileVersionId)
        {
            var info = await dbContext.FileVersions
                .Include(fv => fv.File)
                .Where(fv => fv.FileVersionId == fileVersionId && fv.File.UserId == userId)
                .FirstOrDefaultAsync();

            return info?.ToDto();
        }

        public async Task<FileVersionExtDTO?> GetInfoForUserFileVersionExt(Guid userId, Guid fileVersionId)
        {
            var q = from fv in dbContext.FileVersions
                    join f in dbContext.Files on fv.FileId equals f.FileId
                    where fv.FileVersionId == fileVersionId && f.UserId == userId
                    select new { f, fv };

            var qr = await q.FirstOrDefaultAsync();
            if (qr == null)
            {
                return null;
            }

            var fvext = new FileVersionExtDTO(qr.f.ToDto(), qr.fv.ToDto());

            return fvext;
        }

        public async Task<FileVersionDTO?> GetInfoForFileVersionByVersionNr(Guid fileId, int versionNr)
        {
            var info = await dbContext.FileVersions
                .SingleOrDefaultAsync(fv => fv.FileId == fileId && fv.VersionNr == versionNr);
            return info?.ToDto();
        }

        [Obsolete("Use GetInfoForActiveFileVersion instead")]
        public async Task<FileVersionDTO?> GetInfoForLatestFileVersion(Guid fileId)
        {
            var info = await dbContext.FileVersions
                .Where(fv => fv.FileId == fileId)
                .OrderByDescending(fv => fv.VersionNr)
                .FirstOrDefaultAsync();
            return info?.ToDto();
        }

        [Obsolete("Use GetInfoForAllActiveUserFileVersions instead")]
        public async Task<FileVersionDTO[]> GetInfoForAllLatestUserFileVersions(Guid userId)
        {
            //FIXME client can't distinguish between new and to-be-deleted files; the info for deleted files should still be sent in some way

            var activeUserFiles = dbContext.Files
                .Where(f => f.UserId == userId && !f.Deleted);

            var fileAndMaxVersionNr = dbContext.FileVersions
                .GroupBy(fv => fv.FileId)
                .Select(g => new
                {
                    FileId = g.Key,
                    MaxVersionNr = g.Max(fv => fv.VersionNr)
                });

            var infos = await dbContext.FileVersions
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

            return infos.Select(i => i.ToDto()).ToArray();
        }

        public async Task<FileVersionDTO[]> GetInfoForUserFileVersions(Guid userId, Guid fileId)
        {
            var fvs = await dbContext.FileVersions
                .Include(fv => fv.File)
                .Where(fv => fv.FileId == fileId && fv.File.UserId == userId)
                .ToArrayAsync();

            return fvs.Select(fv => fv.ToDto()).ToArray();
        }

        public async Task<FileVersionDTO?> GetInfoForActiveFileVersion(Guid fileId)
        {
            var fileInfo = await dbContext.Files.FindAsync(fileId);
            if (fileInfo == null)
            {
                return null;
            }

            var fileVersionInfo = await dbContext.FileVersions.FindAsync(fileInfo.ActiveFileVersionId);

            return fileVersionInfo?.ToDto();
        }

        public async Task<FileVersionDTO[]> GetInfoForAllActiveUserFileVersions(Guid userId)
        {
            var q = from file in dbContext.Files
                    join fileVersion in dbContext.FileVersions
                    on file.ActiveFileVersionId equals fileVersion.FileVersionId
                    where file.UserId == userId && !file.Deleted
                    select fileVersion;

            var fvs = await q.ToArrayAsync();

            return fvs.Select(fv => fv.ToDto()).ToArray();
        }

        public async Task<FileVersionDTO[]> GetInfoForAllActiveFileVersionsUnderDirectory(Guid directoryFileId, bool includeDeleted)
        {
            var fileInfo = await dbContext.Files.FindAsync(directoryFileId);
            if (fileInfo == null || !fileInfo.IsDir)
            {
                throw new ArgumentException("Provided file ID does not correspond to a directory or does not exist.", nameof(directoryFileId));
            }

            var activeFileVersionInfo = await GetInfoForActiveFileVersion(directoryFileId) ?? throw new Exception("No active file version found for this directory");

            var userId = fileInfo.UserId;
            var clientDirectoryPath = activeFileVersionInfo.ClientFilePath();

            var clientDirectoryPathUnterminated = clientDirectoryPath.TrimEnd(Path.DirectorySeparatorChar);
            // Make sure path ends with the separator so we don't match other files in the same directory
            // For example if clientDirectoryPath is "foo/bar" without the separator at the end it could match
            // both "foo/bar/baz.txt" and "foo/bar2.txt" files and we don't want that
            var clientDirectoryPathTerminated = clientDirectoryPathUnterminated + Path.DirectorySeparatorChar;

            var q = from file in dbContext.Files
                    join fileVersion in dbContext.FileVersions
                    on file.ActiveFileVersionId equals fileVersion.FileVersionId
                    where file.UserId == userId
                       && (includeDeleted || !file.Deleted)
                       && fileVersion.ClientDirPath != null
                       && (fileVersion.ClientDirPath == clientDirectoryPathUnterminated
                           || fileVersion.ClientDirPath.StartsWith(clientDirectoryPathTerminated))
                    select fileVersion;

            var fvs = await q.ToArrayAsync();

            return fvs.Select(fv => fv.ToDto()).ToArray();
        }

        public async Task<FileVersionExtDTO[]> GetInfoForAllActiveUserFileVersionsExt(Guid userId, bool includeDeleted)
        {
            var activeUserFiles = dbContext.Files
                .Where(f => f.UserId == userId && (includeDeleted || !f.Deleted));

            var q = from file in dbContext.Files
                    join fileVersion in dbContext.FileVersions
                    on file.ActiveFileVersionId equals fileVersion.FileVersionId
                    where file.UserId == userId && (includeDeleted || !file.Deleted)
                    select new { F = file, Fv = fileVersion };

            var qr = await q.ToArrayAsync();

            var dtos = qr.Select(x => new FileVersionExtDTO(x.F.ToDto(), x.Fv.ToDto()));

            return dtos.ToArray();
        }

        public async Task<FileVersionDTO?> GetInfoForUserFileVersionByUniqueContent(Guid userId, string md5Hash, long fileSize)
        {
            var info = await dbContext.FileVersions
                .Include(fv => fv.File)
                .Where(fv => fv.File.UserId == userId && fv.Md5 == md5Hash && fv.SizeBytes == fileSize)
                .FirstOrDefaultAsync();

            return info?.ToDto();
        }
    }
}
