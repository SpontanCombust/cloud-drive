namespace CloudDrive.Core.Domain.Repositories
{
    public interface IFileVersionRepository
    {
        Entities.FileVersion Add(Entities.FileVersion fileVersion);
        Task<IEnumerable<Entities.FileVersion>> GetAllAsync();
        Task<Entities.FileVersion> GetAsync(Guid uuid);
        Task<Entities.FileVersion> UpdateAsync(Guid uuid, Entities.FileVersion fileVersion);
        Task DeleteAsync(Guid uuid);
    }
}
