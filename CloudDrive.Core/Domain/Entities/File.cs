namespace CloudDrive.Core.Domain.Entities
{
    public class File
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public bool Deleted {  get; set; }

        public User User { get; set; }
        public ICollection<FileVersion> FileVersions { get; set; }
    }
}
