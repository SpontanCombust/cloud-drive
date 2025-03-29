using CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Services
{
    public interface IAccessTokenProvider
    {
        string Provide(User user);
    }
}
