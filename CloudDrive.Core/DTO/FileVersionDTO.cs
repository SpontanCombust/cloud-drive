using System.Text.Json.Serialization;

namespace CloudDrive.Core.DTO
{
    public class FileVersionDTO
    {
        public Guid FileVersionId { get; set; }
        public Guid FileId { get; set; }
        /// <summary>
        /// Relative path to the parent directory of this file or directory on the client side
        /// </summary>
        public string? ClientDirPath { get; set; }
        /// <summary>
        /// Name of this file or directory on the client side
        /// </summary>
        public required string ClientFileName { get; set; }
        /// <summary>
        /// Relative path to the parent directory of this file on server's file system; it's null for directories
        /// </summary>
        [JsonIgnore] public string? ServerDirPath { get; set; }
        /// <summary>
        /// Name of this file on the server side; it's null for directories (they do not get stored on the server's file system)
        /// </summary>
        [JsonIgnore] public string? ServerFileName { get; set; }
        /// <summary>
        /// Version number of the file or directory. Starts from zero and goes up by one with each new version.
        /// </summary>
        public int VersionNr { get; set; }
        /// <summary>
        /// MD% hash of the file or null for directories
        /// </summary>
        public string? Md5 { get; set; }
        /// <summary>
        /// Size of the file in bytes or null for directories
        /// </summary>
        public long? SizeBytes { get; set; }
        /// <summary>
        /// UTC creation date of the record on the server
        /// </summary>
        public DateTime CreatedDate { get; set; }


        /// <summary>
        /// Relative path to this file or directory on the client side
        /// </summary>
        public string ClientFilePath()
        {
            return Path.Combine(
                ClientDirPath ?? string.Empty,
                ClientFileName
            );
        }

        /// <summary>
        /// Relative path to this file on the server file system (directories are not stored)
        /// </summary>
        public string? ServerFilePath()
        {
            return (ServerDirPath != null || ServerFileName != null)
                ? Path.Combine(
                    ServerDirPath ?? string.Empty,
                    ServerFileName ?? string.Empty
                )
                : null;
        }
    }
}
