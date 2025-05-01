namespace CloudDrive.WebAPI.Model
{
    public class CreateDirectoryRequest
    {
        /// <summary>
        /// Parent directory path of this directory on the client side (a path relative to the watched folder)
        /// </summary>
        public string? ClientDirPath { get; set; }
        /// <summary>
        /// Name of this directory
        /// </summary>
        public required string ClientFileName { get; set; }
    }
}
