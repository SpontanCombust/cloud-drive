namespace CloudDrive.Core.DTO
{
    public class DeleteDirectoryResultDTO
    {
        public required FileDTO[] AffectedSubfiles;
        public required FileVersionDTO[] AffectedSubfileVersions;
    }
}
