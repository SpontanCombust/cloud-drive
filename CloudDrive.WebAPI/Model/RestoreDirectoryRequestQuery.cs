namespace CloudDrive.WebAPI.Model
{
    public class RestoreDirectoryRequestQuery
    {
        public Guid? FileVersionId { get; set; }
        public bool RestoreSubfiles { get; set; } = false;
    }
}
