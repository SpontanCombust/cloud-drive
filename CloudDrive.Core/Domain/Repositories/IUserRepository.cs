namespace CloudDrive.Core.Domain.Repositories
{
    public interface IUserRepository
    {
        Entities.User Add(Entities.User user);
        Task<IEnumerable<Entities.User>> GetAllAsync();
        Task<Entities.User> GetAsync(Guid uuid);
        Task<Entities.User> UpdateAsync(Guid uuid, Entities.User user);
        Task DeleteAsync(Guid uuid);
    }
}
