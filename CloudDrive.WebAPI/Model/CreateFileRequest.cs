using Microsoft.AspNetCore.Http;

namespace CloudDrive.WebAPI.Model
{
    public class CreateFileRequest
    {
        /// <summary>
        /// Is the requested entity a directory or otherwise a regular file?
        /// </summary>
        public required bool IsDirectory {  get; set; }
        /// <summary>
        /// File contents, if this is a directory it should be null
        /// </summary>
        public IFormFile? File {  get; set; }
        /// <summary>
        /// Parent directory path of this file or directory on the client side (a path relative to the watched folder)
        /// </summary>
        public string? ClientDirPath { get; set; }
        /// <summary>
        /// Name of this file or directory
        /// </summary>
        public required string ClientFileName { get; set; }
    }
}
