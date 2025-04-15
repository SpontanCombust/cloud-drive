namespace CloudDrive.Core.Domain.Entities
{
    public class File
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        /// <summary>
        /// Is this a directory
        /// </summary>
        public bool IsDir { get; set; }
        /// <summary>
        /// Is this file archived
        /// </summary>
        public bool Deleted {  get; set; }

        public User User { get; set; }
    }
}
