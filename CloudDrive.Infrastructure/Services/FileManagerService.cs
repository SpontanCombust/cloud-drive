using CloudDrive.Core.Domain.Entities;
using CloudDrive.Core.Services;
using System.Security.Cryptography;


namespace CloudDrive.Infrastructure.Services
{
    public class FileManagerService : IFileManagerService
    {
        private readonly IUserService userService;
        private readonly IFileInfoService fileInfoService;
        private readonly IFileVersionInfoService fileVersionInfoService;
        private readonly IFileSystemService fileSystemService;

        public FileManagerService(
            IUserService userService, 
            IFileInfoService fileInfoService, 
            IFileVersionInfoService fileVersionInfoService, 
            IFileSystemService fileSystemService)
        {
            this.userService = userService;
            this.fileInfoService = fileInfoService;
            this.fileVersionInfoService = fileVersionInfoService;
            this.fileSystemService = fileSystemService;
        }


        public async Task<CreateFileResult> CreateFile(Guid userId, Stream inputStream, string fileName, string clientDirPath)
        {
            User? user = await userService.GetUserById(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User could not be found");
            }

            Guid newFileId = Guid.NewGuid();
            Guid newFileVersionId = Guid.NewGuid();
            string newFileVersionServerPath = await fileSystemService.AllocatePathForFile(userId, newFileVersionId);
            string newFileVersionServerDirPath = Path.GetDirectoryName(newFileVersionServerPath)!;
            string newFileVersionServerFileName = Path.GetFileName(newFileVersionServerPath);

            await fileSystemService.CreateFile(newFileVersionServerPath, inputStream);

            inputStream.Seek(0, SeekOrigin.Begin);
            string hash = await CalculateFileHash(inputStream);

            inputStream.Seek(0, SeekOrigin.Begin);
            long fileSize = inputStream.Length;

            var fileInfo = await fileInfoService.CreateInfoForNewFile(newFileId, userId);
            var fileVersionInfo = await fileVersionInfoService.CreateInfoForNewFileVersion(
                newFileVersionId,
                newFileId,
                clientDirPath,
                fileName,
                newFileVersionServerDirPath,
                newFileVersionServerFileName,
                hash,
                fileSize
            );

            var result = new CreateFileResult
            {
                FileInfo = fileInfo,
                FirstFileVersionInfo = fileVersionInfo,
            };

            return result;
        }

        public async Task<GetFileResult?> GetFileVersion(Guid fileId, int versionNr)
        {
            var info = await fileVersionInfoService.GetInfoForFileVersionByVersionNr(fileId, versionNr);
            if (info == null)
            {
                return null;
            }

            string filePath = Path.Combine(info.ServerDirPath, info.ServerFileName);
            byte[]? fileContent = await fileSystemService.GetFile(filePath);
            if (fileContent == null)
            {
                return null;
            }

            var result = new GetFileResult
            {
                FileContent = fileContent,
                ClientDirPath = info.ClientDirPath,
                ClientFileName = info.ClientFileName
            };

            return result;
        }

        public async Task<GetFileResult?> GetLatestFileVersion(Guid fileId)
        {
            var info = await fileVersionInfoService.GetInfoForLatestFileVersion(fileId);
            if (info == null)
            {
                return null;
            }

            string filePath = Path.Combine(info.ServerDirPath, info.ServerFileName);
            byte[]? fileContent = await fileSystemService.GetFile(filePath);
            if (fileContent == null)
            {
                return null;
            }

            var result = new GetFileResult
            {
                FileContent = fileContent,
                ClientDirPath = info.ClientDirPath,
                ClientFileName = info.ClientFileName
            };

            return result;
        }


        private static async Task<string> CalculateFileHash(Stream fileStream)
        {
            var hashBytes = await MD5.HashDataAsync(fileStream);
            var hashStr = Convert.ToHexString(hashBytes);
            return hashStr;
        }
    }
}
