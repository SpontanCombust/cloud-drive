using Microsoft.AspNetCore.Http;

namespace CloudDrive.WebAPI.Model
{
    public class CreateFileRequest
    {
        public required bool IsDirectory {  get; set; }
        public IFormFile? File {  get; set; }
        public string? ClientDirPath { get; set; }
        public required string ClientFileName { get; set; }
    }
}
