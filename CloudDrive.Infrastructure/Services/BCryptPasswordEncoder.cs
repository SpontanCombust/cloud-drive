using CloudDrive.Core.Services;

namespace CloudDrive.Infrastructure.Services
{
    public class BCryptPasswordEncoder : IPasswordEncoder
    {
        public string Encode(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
