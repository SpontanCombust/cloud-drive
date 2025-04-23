using Microsoft.AspNetCore.Http;

namespace CloudDrive.WebAPI.Model
{
    public class CreateFileRequest
    {
        public required IFormFile File {  get; set; }
        public string? ClientDirPath { get; set; }
    }
}
