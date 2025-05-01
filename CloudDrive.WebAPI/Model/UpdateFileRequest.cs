namespace CloudDrive.WebAPI.Model
{
    public class UpdateFileRequest
    {
        public IFormFile? File { get; set; }
        public string? ClientDirPath { get; set; }
        public required string ClientFileName { get; set; }
    }
}
