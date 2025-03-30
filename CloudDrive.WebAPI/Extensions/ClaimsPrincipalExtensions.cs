using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CloudDrive.WebAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetId(this ClaimsPrincipal principal)
        {
            string sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "";
            return Guid.Parse(sub);
        }
    }
}
