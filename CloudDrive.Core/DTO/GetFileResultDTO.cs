namespace CloudDrive.Core.DTO
{
    public class GetFileResultDTO
    {
        /// <summary>
        /// Content of the file encoded as a byte array; this is null if this file is a directory
        /// </summary>
        public byte[]? FileContent { get; set; }
        /// <summary>
        /// Name of this file or directory on the client side
        /// </summary>
        public required string ClientFileName { get; set; }
        /// <summary>
        /// Relative path to the parent directory of this file or directory on the client side
        /// </summary>
        public string? ClientDirPath { get; set; }
    }
}
