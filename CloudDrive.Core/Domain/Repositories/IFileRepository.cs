namespace CloudDrive.Core.Domain.Repositories
{
    public interface IFileRepository
    {
        Entities.File Add(Entities.File file);
        Task<IEnumerable<Entities.File>> GetAllAsync();
        Task<Entities.File> GetAsync(Guid uuid);
        Task<Entities.File> UpdateAsync(Guid uuid, Entities.File file);
        Task DeleteAsync(Guid uuid);
    }
}
