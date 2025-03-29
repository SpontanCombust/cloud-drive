namespace CloudDrive.Core.Services
{
    public interface IAuthService
    {
        Task SignUp(string email, string password);
        Task<string> SignIn(string email, string password);
    }
}
