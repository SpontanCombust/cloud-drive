namespace CloudDrive.Core.Services
{
    public interface IFileSystemService
    {
        /// <summary>
        /// Returns relative server file path
        /// </summary>
        Task<string> AllocatePathForFile(Guid userId, Guid fileVersionId);
        Task CreateFile(string relativeServerFilePath, Stream fileStream);
        Task<byte[]?> GetFile(string relativeServerFilePath);
        Task DeleteFile(string relativeServerFilePath);
    }
}
