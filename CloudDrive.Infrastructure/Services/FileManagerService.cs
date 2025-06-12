using CloudDrive.Core.Domain.Entities;
using CloudDrive.Core.DTO;
using CloudDrive.Core.Services;
using System.IO;
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


        public async Task<CreateFileResultDTO> CreateFile(Guid userId, Stream inputStream, string fileName, string? clientDirPath)
        {
            User? user = await userService.GetUserById(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User could not be found");
            }
            if (inputStream == null)
            {
                throw new InvalidOperationException("File stream is required");
            }

            //FIXME make sure there are no local path conflicts between different active files

            Guid newFileId = Guid.NewGuid();
            Guid newFileVersionId = Guid.NewGuid();
            string newFileVersionServerPath = await fileSystemService.AllocatePathForFile(userId, newFileVersionId);
            string newFileVersionServerDirPath = Path.GetDirectoryName(newFileVersionServerPath)!;
            string newFileVersionServerFileName = Path.GetFileName(newFileVersionServerPath);

            inputStream.Seek(0, SeekOrigin.Begin);
            string hash = await CalculateFileHash(inputStream);

            inputStream.Seek(0, SeekOrigin.Begin);
            long fileSize = inputStream.Length;

            inputStream.Seek(0, SeekOrigin.Begin);
            await fileSystemService.CreateFile(newFileVersionServerPath, inputStream);

            var fileInfo = await fileInfoService.CreateInfoForNewFile(newFileId, userId, false, newFileVersionId);

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

            var result = new CreateFileResultDTO
            {
                FileInfo = fileInfo,
                FirstFileVersionInfo = fileVersionInfo,
            };

            return result;
        }

        public async Task<GetFileResultDTO?> GetFileVersion(Guid fileId, int versionNr)
        {
            var info = await fileInfoService.GetInfoForFile(fileId);
            if (info == null)
            {
                return null;
            }
            if (info.IsDir)
            {
                throw new Exception("Requested file is not a regular file and instead a directory");
            }

            var verInfo = await fileVersionInfoService.GetInfoForFileVersionByVersionNr(fileId, versionNr);
            if (verInfo == null)
            {
                return null;
            }

            byte[]? fileContent = null;
            if (verInfo.ServerDirPath != null && verInfo.ServerFileName != null)
            {
                string filePath = Path.Combine(verInfo.ServerDirPath, verInfo.ServerFileName);
                fileContent = await fileSystemService.GetFile(filePath);
            }

            var result = new GetFileResultDTO
            {
                FileContent = fileContent,
                ClientDirPath = verInfo.ClientDirPath,
                ClientFileName = verInfo.ClientFileName
            };

            return result;
        }

        [Obsolete("Use GetActiveFileVersion instead")]
        public async Task<GetFileResultDTO?> GetLatestFileVersion(Guid fileId)
        {
            var info = await fileInfoService.GetInfoForFile(fileId);
            if (info == null)
            {
                return null;
            }
            if (info.IsDir)
            {
                throw new Exception("Requested file is not a regular file and instead a directory");
            }

            var verInfo = await fileVersionInfoService.GetInfoForLatestFileVersion(fileId);
            if (verInfo == null)
            {
                return null;
            }

            byte[]? fileContent = null;
            if (verInfo.ServerDirPath != null && verInfo.ServerFileName != null)
            {
                string filePath = Path.Combine(verInfo.ServerDirPath, verInfo.ServerFileName);
                fileContent = await fileSystemService.GetFile(filePath);
            }

            var result = new GetFileResultDTO
            {
                FileContent = fileContent,
                ClientDirPath = verInfo.ClientDirPath,
                ClientFileName = verInfo.ClientFileName
            };

            return result;
        }

        public async Task<GetFileResultDTO?> GetActiveFileVersion(Guid fileId)
        {
            var info = await fileInfoService.GetInfoForFile(fileId);
            if (info == null)
            {
                return null;
            }
            if (info.IsDir)
            {
                throw new Exception("Requested file is not a regular file and instead a directory");
            }

            var verInfo = await fileVersionInfoService.GetInfoForActiveFileVersion(fileId);
            if (verInfo == null)
            {
                return null;
            }

            byte[]? fileContent = null;
            if (verInfo.ServerDirPath != null && verInfo.ServerFileName != null)
            {
                string filePath = Path.Combine(verInfo.ServerDirPath, verInfo.ServerFileName);
                fileContent = await fileSystemService.GetFile(filePath);
            }

            var result = new GetFileResultDTO
            {
                FileContent = fileContent,
                ClientDirPath = verInfo.ClientDirPath,
                ClientFileName = verInfo.ClientFileName
            };

            return result;
        }

        public async Task<FileVersionDTO> UpdateFile(Guid fileId, Stream fileStream, string clientFileName, string? clientDirPath)
        {
            if (fileStream == null)
            {
                throw new InvalidOperationException("File stream is required");
            }

            //TODO create dedicated exception type
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("File does not exist");

            if (fileInfo.Deleted)
            {
                throw new Exception("File is marked as deleted and cannot be updated");
            }
            if (fileInfo.IsDir)
            {
                throw new Exception("Requested file is not a regular file and instead a directory");
            }


            Guid newFileVersionId = Guid.NewGuid();
            string newFileVersionServerPath = await fileSystemService.AllocatePathForFile(fileInfo.UserId, newFileVersionId);
            string newFileVersionServerDirPath = Path.GetDirectoryName(newFileVersionServerPath)!;
            string newFileVersionServerFileName = Path.GetFileName(newFileVersionServerPath);

            fileStream.Seek(0, SeekOrigin.Begin);
            string hash = await CalculateFileHash(fileStream);

            fileStream.Seek(0, SeekOrigin.Begin);
            long? fileSize = fileStream.Length;

            fileStream.Seek(0, SeekOrigin.Begin);
            await fileSystemService.CreateFile(newFileVersionServerPath, fileStream);

            //FIXME make sure there are no local path conflicts between different active files

            await fileInfoService.UpdateInfoForFile(fileId, null, newFileVersionId);

            var fileVersionInfo = await fileVersionInfoService.CreateInfoForNewFileVersion(
                newFileVersionId,
                fileId,
                clientDirPath,
                clientFileName,
                newFileVersionServerDirPath,
                newFileVersionServerFileName,
                hash,
                fileSize
            );

            return fileVersionInfo;
        }

        public async Task DeleteFile(Guid fileId)
        {
            await fileInfoService.UpdateInfoForFile(fileId, deleted: true, activeFileVersionId: null);
        }

        public async Task<RestoreFileResultDTO> RestoreFile(Guid fileId)
        {
            var fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: null);
            var fileVersionInfo = await fileVersionInfoService.GetInfoForActiveFileVersion(fileId) ?? throw new Exception("No active file version found for the restored file");

            var result = new RestoreFileResultDTO
            {
                FileInfo = fileInfo,
                ActiveFileVersionInfo = fileVersionInfo
            };

            return result;
        }

        public async Task<RestoreFileResultDTO> RestoreFile(Guid fileId, Guid fileVersionId)
        {
            var fileVersionInfo = await fileVersionInfoService.GetInfoForFileVersion(fileVersionId) ?? throw new Exception("File version not found");

            if (fileVersionInfo.FileId != fileId)
            {
                throw new Exception("File version does not belong to the specified file");
            }

            var fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: fileVersionId);

            var result = new RestoreFileResultDTO
            {
                FileInfo = fileInfo,
                ActiveFileVersionInfo = fileVersionInfo
            };

            return result;
        }



        public async Task<CreateDirectoryResultDTO> CreateDirectory(Guid userId, string fileName, string? clientDirPath)
        {
            User? user = await userService.GetUserById(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User could not be found");
            }

            //FIXME make sure there are no local path conflicts between different active files

            Guid newFileId = Guid.NewGuid();
            Guid newFileVersionId = Guid.NewGuid();

            var fileInfo = await fileInfoService.CreateInfoForNewFile(newFileId, userId, true, newFileVersionId);

            var fileVersionInfo = await fileVersionInfoService.CreateInfoForNewFileVersion(
                newFileVersionId,
                newFileId,
                clientDirPath,
                fileName,
                null,
                null,
                null,
                null
            );

            var result = new CreateDirectoryResultDTO
            {
                FileInfo = fileInfo,
                FirstFileVersionInfo = fileVersionInfo,
            };

            return result;
        }

        public async Task<FileVersionDTO> UpdateDirectory(Guid fileId, string clientFileName, string? clientDirPath)
        {
            //TODO create dedicated exception type
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("File does not exist");

            if (fileInfo.Deleted)
            {
                throw new Exception("File is marked as deleted and cannot be updated");
            }
            if (!fileInfo.IsDir)
            {
                throw new Exception("Requested file is not a directory and instead a regular file");
            }


            Guid newFileVersionId = Guid.NewGuid();

            //FIXME make sure there are no local path conflicts between different active files

            await fileInfoService.UpdateInfoForFile(fileId, null, newFileVersionId);

            var fileVersionInfo = await fileVersionInfoService.CreateInfoForNewFileVersion(
                newFileVersionId,
                fileId,
                clientDirPath,
                clientFileName,
                null,
                null,
                null,
                null
            );

            //FIXME dependant files not affected!

            return fileVersionInfo;
        }

        public async Task DeleteDirectory(Guid fileId)
        {
            await fileInfoService.UpdateInfoForFile(fileId, deleted: true, activeFileVersionId: null);

            //FIXME dependant files not affected!
        }

        public async Task<RestoreDirectoryResultDTO> RestoreDirectory(Guid fileId)
        {
            var fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: null);
            var fileVersionInfo = await fileVersionInfoService.GetInfoForActiveFileVersion(fileId) ?? throw new Exception("No active file version found for the restored file");

            var result = new RestoreDirectoryResultDTO
            {
                FileInfo = fileInfo,
                ActiveFileVersionInfo = fileVersionInfo
            };

            //FIXME dependant files never taken into account!

            return result;
        }

        public async Task<RestoreDirectoryResultDTO> RestoreDirectory(Guid fileId, Guid fileVersionId)
        {
            var fileVersionInfo = await fileVersionInfoService.GetInfoForFileVersion(fileVersionId) ?? throw new Exception("File version not found");

            if (fileVersionInfo.FileId != fileId)
            {
                throw new Exception("File version does not belong to the specified file");
            }

            var fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: fileVersionId);

            var result = new RestoreDirectoryResultDTO
            {
                FileInfo = fileInfo,
                ActiveFileVersionInfo = fileVersionInfo
            };

            //FIXME dependant files never taken into account!

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
