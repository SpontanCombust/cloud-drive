namespace CloudDrive.WebAPI.Model
{
    public class UpdateFileRequest
    {
        /// <summary>
        /// File contents
        /// </summary>
        public required IFormFile File { get; set; }
        /// <summary>
        /// Parent directory path of this regular file on the client side (a path relative to the watched folder)
        /// </summary>
        public string? ClientDirPath { get; set; }
    }
}
