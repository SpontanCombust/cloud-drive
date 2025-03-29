using CloudDrive.Core.Domain.Entities;
using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Repositories;
using CloudDrive.WebAPI.Security;
using Microsoft.EntityFrameworkCore;

namespace CloudDrive.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext dbContext;
        private readonly IPasswordEncoder passwordEncoder;
        private readonly IAccessTokenProvider tokenProvider;

        public AuthService(AppDbContext dbContext, IPasswordEncoder passwordEncoder, IAccessTokenProvider tokenProvider)
        {
            this.dbContext = dbContext;
            this.passwordEncoder = passwordEncoder;
            this.tokenProvider = tokenProvider;
        }


        public async Task SignUp(string email, string password)
        {
            User? existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null)
            {
                throw new Exception("This email is already in use");
            }

            User newUser = new()
            {
                UserId = new Guid(),
                Email = email,
                Password = passwordEncoder.Encode(password),
                CreatedDate = new DateTime().ToUniversalTime()
            };

            await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();
        }

        public async Task<string> SignIn(string email, string password)
        {
            User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new Exception("Invalid credentials");
            }

            if (!passwordEncoder.Verify(password, user.Password))
            {
                throw new Exception("Invalid credentials");
            }

            var token = tokenProvider.Provide(user);

            return token;
        }
    }
}
