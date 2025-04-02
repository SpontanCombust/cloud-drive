using CloudDrive.Core.Services;
using Microsoft.Extensions.Configuration;


namespace CloudDrive.Infrastructure.Services
{
    public class FileSystemService : IFileSystemService
    {
        private readonly IConfiguration configuration;

        public FileSystemService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }


        public Task<string> AllocatePathForFile(Guid userId, Guid fileVersionId)
        {
            string userDirRelativePath = userId.ToString("N");
            string fileRelativePath = Path.Combine(userDirRelativePath, fileVersionId.ToString("N"));

            string userDirFullPath = ComputeFullPathForFile(userDirRelativePath);
            Directory.CreateDirectory(userDirFullPath);

            return Task.FromResult(fileRelativePath);
        }

        public async Task CreateFile(string serverFilePath, Stream inputStream)
        {
            string fullPath = ComputeFullPathForFile(serverFilePath);
            using var outputStream = new FileStream(fullPath, FileMode.Create);
            await inputStream.CopyToAsync(outputStream);
        }

        public async Task<byte[]?> GetFile(string serverFilePath)
        {
            string fullPath = ComputeFullPathForFile(serverFilePath);
            if (File.Exists(fullPath))
            {
                var fileBytes = await File.ReadAllBytesAsync(fullPath);
                return fileBytes;
            }

            return null;
        }

        public Task DeleteFile(string serverFilePath)
        {
            string fullPath = ComputeFullPathForFile(serverFilePath);
            File.Delete(fullPath);
            return Task.CompletedTask;
        }



        private string GetFsRootFullPath()
        {
            var fsRootStr = configuration["fsRoot"];
            if (fsRootStr == null)
            {
                throw new Exception("Server file system root is not configured");
            }
            else if  (!Path.Exists(fsRootStr) || !Path.IsPathRooted(fsRootStr))
            {
                throw new Exception("Server file system root should be an existing, absolute path");
            }

            return fsRootStr;
        }

        private string ComputeFullPathForFile(string relativeFilePath)
        {
            return Path.Combine(GetFsRootFullPath(), relativeFilePath);
        }
    }
}
