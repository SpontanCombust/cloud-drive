using Microsoft.AspNetCore.Http;

namespace CloudDrive.Infrastructure.Commands
{
    public class CreateFileRequest
    {
        public required IFormFile File {  get; set; }
        public required string ClientDirPath { get; set; }
    }
}
