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
    }
}
