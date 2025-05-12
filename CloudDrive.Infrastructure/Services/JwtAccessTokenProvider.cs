using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Extensions.Configuration;
using CloudDrive.Core.Services;

namespace CloudDrive.WebAPI.Security
{
    public class JwtAccessTokenProvider : IAccessTokenProvider
    {
        private IConfiguration configuration;

        public JwtAccessTokenProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Provide(Guid userId, string userEmail)
        {
            string secretKey = configuration.GetValue<string>("Jwt:Secret") ?? "";
            int keyExpiration = configuration.GetValue<int>("Jwt:ExpirationMinutes");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, userEmail)
                ]),
                Expires = DateTime.UtcNow.AddMinutes(keyExpiration),
                SigningCredentials = credentials
            };

            var handler = new JsonWebTokenHandler();
            string token = handler.CreateToken(tokenDescriptor);

            return token;
        }
    }
}
