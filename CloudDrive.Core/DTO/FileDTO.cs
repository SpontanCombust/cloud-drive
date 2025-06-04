namespace CloudDrive.Core.DTO
{
    public class FileDTO
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        /// <summary>
        /// Is this a directory
        /// </summary>
        public required bool IsDir { get; set; }
        /// <summary>
        /// Is this file archived
        /// </summary>
        public bool Deleted { get; set; }
        /// <summary>
        /// Which version of the file should be used by clients if the file is not deleted
        /// </summary>
        public Guid ActiveFileVersionId { get; set; }
        /// <summary>
        /// UTC creation date of the record on the server
        /// </summary>
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// UTC creation date of the record on the server
        /// </summary>
        public DateTime? ModifiedDate { get; set; }
    }
}
