namespace CloudDrive.Core.Services
{
    public interface IFileManagerService
    {
        Task<CreateFileResult> CreateFile(Guid userId, Stream inputStream, string fileName, string clientDirPath);
        Task<byte[]?> GetFileVersion(Guid fileId, int versionNr);
        Task<byte[]?> GetLatestFileVersion(Guid fileId);
    }
}
