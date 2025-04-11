namespace CloudDrive.Core.Services
{
    public interface IAccessTokenProvider
    {
        string Provide(Guid userId, string userEmail);
    }
}
