using CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Services
{
    public interface IUserService
    {
        Task<User?> GetUserById(Guid id);
        Task<User?> GetUserByEmail(string email);
    }
}
