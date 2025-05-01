namespace CloudDrive.Core.Domain.Entities
{
    public class FileVersion
    {
        public Guid FileVersionId { get; set; }
        public Guid FileId {  get; set; }
        /// <summary>
        /// Relative path to the parent directory of this file or directory on the client side
        /// </summary>
        public string? ClientDirPath {  get; set; }
        /// <summary>
        /// Name of this file or directory on the client side
        /// </summary>
        public required string ClientFileName { get; set; }
        /// <summary>
        /// Relative path to the parent directory of this file on server's file system; it's null for directories
        /// </summary>
        public string? ServerDirPath { get; set; }
        /// <summary>
        /// Name of this file on the server side; it's null for directories (they do not get stored on the server's file system)
        /// </summary>
        public string? ServerFileName { get; set; }
        /// <summary>
        /// Version number of the file or directory. Starts from zero and goes up by one with each new version.
        /// </summary>
        public int VersionNr {  get; set; }
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

        public File File { get; set; }
    }
}
