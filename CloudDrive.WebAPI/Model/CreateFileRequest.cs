using Microsoft.AspNetCore.Http;

namespace CloudDrive.WebAPI.Model
{
    public class CreateFileRequest
    {
        public required IFormFile File {  get; set; }
        public required string ClientDirPath { get; set; }
    }
}
