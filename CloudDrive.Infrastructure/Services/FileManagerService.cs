using CloudDrive.Core.Domain.Entities;
using CloudDrive.Core.DTO;
using CloudDrive.Core.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;


namespace CloudDrive.Infrastructure.Services
{
    public class FileManagerService : IFileManagerService
    {
        private readonly ILogger<FileManagerService> logger;
        private readonly IUserService userService;
        private readonly IFileInfoService fileInfoService;
        private readonly IFileVersionInfoService fileVersionInfoService;
        private readonly IFileSystemService fileSystemService;

        public FileManagerService(
            ILogger<FileManagerService> logger,
            IUserService userService, 
            IFileInfoService fileInfoService, 
            IFileVersionInfoService fileVersionInfoService, 
            IFileSystemService fileSystemService)
        {
            this.logger = logger;
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

            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(userId, clientDirPath, fileName))
            {
                throw new Exception("There already currently exists a file or directory at the specified client path");
            }

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
                string? filePath = verInfo.ServerFilePath();
                if (filePath == null)
                {
                    return null;
                }

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
                string? filePath = verInfo.ServerFilePath();
                if (filePath == null)
                {
                    return null;
                }

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
                string? filePath = verInfo.ServerFilePath();
                if (filePath == null)
                {
                    return null;
                }

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

        public async Task<UpdateFileResultDTO> UpdateFile(Guid fileId, Stream fileStream, string clientFileName, string? clientDirPath)
        {
            if (fileStream == null)
            {
                throw new InvalidOperationException("File stream is required");
            }

            //TODO create dedicated exception type
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("File does not exist");
            var oldFileVersionInfo = await fileVersionInfoService.GetInfoForActiveFileVersion(fileId) ?? throw new Exception("Active file version not assigned");

            if (fileInfo.Deleted)
            {
                throw new Exception("File is marked as deleted and cannot be updated");
            }
            if (fileInfo.IsDir)
            {
                throw new Exception("Requested file is not a regular file and instead a directory");
            }
            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(fileInfo.UserId, clientDirPath, clientFileName))
            {
                throw new Exception("There already currently exists another file or directory at the specified client path");
            }


            fileStream.Seek(0, SeekOrigin.Begin);
            string hash = await CalculateFileHash(fileStream);

            fileStream.Seek(0, SeekOrigin.Begin);
            long? fileSize = fileStream.Length;


            bool changed =
                   clientFileName != oldFileVersionInfo.ClientFileName
                || clientDirPath != oldFileVersionInfo.ClientDirPath
                || hash != oldFileVersionInfo.Md5
                || fileSize != oldFileVersionInfo.SizeBytes;


            if (changed)
            {
                Guid newFileVersionId = Guid.NewGuid();
                string newFileVersionServerPath = await fileSystemService.AllocatePathForFile(fileInfo.UserId, newFileVersionId);
                string newFileVersionServerDirPath = Path.GetDirectoryName(newFileVersionServerPath)!;
                string newFileVersionServerFileName = Path.GetFileName(newFileVersionServerPath);

                fileStream.Seek(0, SeekOrigin.Begin);
                await fileSystemService.CreateFile(newFileVersionServerPath, fileStream);

                var newFileVersionInfo = await fileVersionInfoService.CreateInfoForNewFileVersion(
                    newFileVersionId,
                    fileId,
                    clientDirPath,
                    clientFileName,
                    newFileVersionServerDirPath,
                    newFileVersionServerFileName,
                    hash,
                    fileSize
                );

                await fileInfoService.UpdateInfoForFile(fileId, null, newFileVersionId);

                return new UpdateFileResultDTO
                {
                    Changed = true,
                    ActiveFileVersion = newFileVersionInfo,
                };
            }
            else
            {
                return new UpdateFileResultDTO
                {
                    Changed = false,
                    ActiveFileVersion = oldFileVersionInfo,
                };
            }
        }

        public async Task DeleteFile(Guid fileId)
        {
            await fileInfoService.UpdateInfoForFile(fileId, deleted: true, activeFileVersionId: null);
        }

        public async Task<RestoreFileResultDTO> RestoreFile(Guid fileId)
        {
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("File not found");
            if (!fileInfo.IsDir)
            {
                throw new ArgumentException("Specified file ID belongs to a directory and not a regular file");
            }

            var fileVersionInfo = await fileVersionInfoService.GetInfoForActiveFileVersion(fileId) ?? throw new Exception("No active file version found for the restored file");

            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(fileInfo.UserId, fileVersionInfo.ClientDirPath, fileVersionInfo.ClientFileName))
            {
                throw new Exception("There already currently exists a file or directory at the path of the file to be restored");
            }


            fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: null);


            var result = new RestoreFileResultDTO
            {
                FileInfo = fileInfo,
                ActiveFileVersionInfo = fileVersionInfo
            };

            return result;
        }

        public async Task<RestoreFileResultDTO> RestoreFile(Guid fileId, Guid fileVersionId)
        {
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("File not found");
            if (!fileInfo.IsDir)
            {
                throw new ArgumentException("Specified file ID belongs to a directory and not a regular file");
            }

            var fileVersionInfo = await fileVersionInfoService.GetInfoForFileVersion(fileVersionId) ?? throw new Exception("File version not found");

            if (fileVersionInfo.FileId != fileId)
            {
                throw new Exception("File version does not belong to the specified file");
            }
            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(fileInfo.UserId, fileVersionInfo.ClientDirPath, fileVersionInfo.ClientFileName))
            {
                throw new Exception("There already currently exists a file or directory at the path of the file version to be restored");
            }

            fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: fileVersionId);

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

            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(userId, clientDirPath, fileName))
            {
                throw new Exception("There already currently exists a file or directory at the specified client path");
            }

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

        public async Task<UpdateDirectoryResultDTO> UpdateDirectory(Guid fileId, string clientFileName, string? clientDirPath)
        {
            //TODO create dedicated exception type
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("File does not exist");
            var oldFileVersionInfo = await fileVersionInfoService.GetInfoForActiveFileVersion(fileId) ?? throw new Exception("Active file version not assigned");

            if (fileInfo.Deleted)
            {
                throw new Exception("File is marked as deleted and cannot be updated");
            }
            if (!fileInfo.IsDir)
            {
                throw new Exception("Requested file is not a directory and instead a regular file");
            }
            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(fileInfo.UserId, clientDirPath, clientFileName))
            {
                throw new Exception("There already currently exists another file or directory at the specified client path of the directory");
            }


            bool changed =
                clientFileName != oldFileVersionInfo.ClientFileName
             || clientDirPath != oldFileVersionInfo.ClientDirPath; 


            if (changed)
            {
                Guid newFileVersionId = Guid.NewGuid();

                var subfileVersionInfos = await fileVersionInfoService.GetInfoForAllActiveFileVersionsUnderDirectory(fileId, false);

                var newFileVersionInfo = await fileVersionInfoService.CreateInfoForNewFileVersion(
                    newFileVersionId,
                    fileId,
                    clientDirPath,
                    clientFileName,
                    null,
                    null,
                    null,
                    null
                );

                await fileInfoService.UpdateInfoForFile(fileId, null, newFileVersionId);


                // updating paths of subfiles

                var newSubfileVersionInfosExt = new List<FileVersionExtDTO>(subfileVersionInfos.Length);

                foreach (var fv in subfileVersionInfos)
                {
                    var newSubfileVersionId = Guid.NewGuid();
                    var newSubfileVersionClientDirPath = UpdatedDirectorySubfileClientDirPath(fv, oldFileVersionInfo, newFileVersionInfo);

                    var newSubfileVersionInfo = await fileVersionInfoService.CreateInfoForNewFileVersion(
                        newSubfileVersionId,
                        fv.FileId,
                        newSubfileVersionClientDirPath,
                        fv.ClientFileName,
                        fv.ServerDirPath,
                        fv.ServerFileName,
                        fv.Md5,
                        fv.SizeBytes
                    );

                    var subfileInfo = await fileInfoService.UpdateInfoForFile(newSubfileVersionInfo.FileId, null, newSubfileVersionInfo.FileVersionId);

                    newSubfileVersionInfosExt.Add(new FileVersionExtDTO(subfileInfo, newSubfileVersionInfo));
                }

                return new UpdateDirectoryResultDTO
                {
                    Changed = true,
                    ActiveFileVersion = newFileVersionInfo,
                    NewSubfileVersionsExt = newSubfileVersionInfosExt.ToArray(),
                };
            }
            else
            {
                return new UpdateDirectoryResultDTO
                {
                    Changed = false,
                    ActiveFileVersion = oldFileVersionInfo,
                    NewSubfileVersionsExt = []
                };
            }
        }

        private string? UpdatedDirectorySubfileClientDirPath(FileVersionDTO prevSubfileVersion, FileVersionDTO prevAncestorDirFileVersion, FileVersionDTO updatedAncestorDirFileVersion)
        {
            string prevAncestorPath = prevAncestorDirFileVersion.ClientFilePath();

            if (prevSubfileVersion.ClientDirPath == null || !prevSubfileVersion.ClientDirPath.StartsWith(prevAncestorPath))
            {
                logger.LogError("Previous subfile version's ({SubfileVersionId}) parent directory path does not contain the previous ancestor directory path: {SubfileParentDirPath} does not contain {AncestorDirPath}",
                    prevSubfileVersion.FileVersionId, prevSubfileVersion.ClientDirPath, prevAncestorPath);

                return prevSubfileVersion.ClientDirPath;
            }

            string newAncestorPath = updatedAncestorDirFileVersion.ClientFilePath();
            string prevSubfileParentDirPathAfterAncestor = prevSubfileVersion.ClientDirPath.Substring(prevAncestorPath.Length);
            string newSubfileParentDirPath = string.Concat(newAncestorPath, prevSubfileParentDirPathAfterAncestor);

            return newSubfileParentDirPath;
        }


        public async Task<DeleteDirectoryResultDTO> DeleteDirectory(Guid fileId)
        {
            if (!await fileInfoService.FileIsDirectory(fileId))
            {
                throw new ArgumentException("Requested file is not a directory and instead a regular file", nameof(fileId));
            }

            await fileInfoService.UpdateInfoForFile(fileId, deleted: true, activeFileVersionId: null);


            var subfileFileVersions = await fileVersionInfoService.GetInfoForAllActiveFileVersionsUnderDirectory(fileId, false);
            var fileIds = subfileFileVersions.Select(fv => fv.FileId).ToArray();
            var files = await fileInfoService.GetInfoForManyFiles(fileIds);

            foreach (var f in files)
            {
                f.Deleted = true;
            }

            await fileInfoService.UpdateInfoForManyFiles(files);

            var result = new DeleteDirectoryResultDTO
            {
                AffectedSubfiles = files,
                AffectedSubfileVersions = subfileFileVersions
            };

            return result;
        }

        public async Task<RestoreDirectoryResultDTO> RestoreDirectory(Guid fileId)
        {
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("Directory not found");
            if (!fileInfo.IsDir)
            {
                throw new ArgumentException("Specified file ID belongs to a regular file and not a directory");
            }

            var fileVersionInfo = await fileVersionInfoService.GetInfoForActiveFileVersion(fileId) ?? throw new Exception("No active file version found for the restored directory");

            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(fileInfo.UserId, fileVersionInfo.ClientDirPath, fileVersionInfo.ClientFileName))
            {
                throw new Exception("There already exists an actively used file or directory at the path of the directory to be restored");
            }

            fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: null);

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
            var fileInfo = await fileInfoService.GetInfoForFile(fileId) ?? throw new Exception("Directory not found");
            if (!fileInfo.IsDir)
            {
                throw new ArgumentException("Specified file ID belongs to a regular file and not a directory");
            }

            var fileVersionInfo = await fileVersionInfoService.GetInfoForFileVersion(fileVersionId) ?? throw new Exception("File version not found");

            if (fileVersionInfo.FileId != fileId)
            {
                throw new Exception("File version does not belong to the specified file");
            }
            if (await fileVersionInfoService.ExistsPresentActiveUserFileVersionWithClientPath(fileInfo.UserId, fileVersionInfo.ClientDirPath, fileVersionInfo.ClientFileName))
            {
                throw new Exception("There already currently exists a file or directory at the path of the directory version to be restored");
            }

            fileInfo = await fileInfoService.UpdateInfoForFile(fileId, deleted: false, activeFileVersionId: fileVersionId);

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
