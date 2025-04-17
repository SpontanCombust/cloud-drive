namespace CloudDrive.Core.DTO
{
    public class FileDTO
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public bool Deleted { get; set; }
    }
}
