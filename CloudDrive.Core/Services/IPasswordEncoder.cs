namespace CloudDrive.Core.Services
{
    public interface IPasswordEncoder
    {
        string Encode(string password);
        bool Verify(string password, string passwordHash);
    }
}
