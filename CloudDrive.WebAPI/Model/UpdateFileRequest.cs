namespace CloudDrive.WebAPI.Model
{
    public class UpdateFileRequest
    {
        public required IFormFile File { get; set; }
        public string? ClientDirPath { get; set; }
    }
}
