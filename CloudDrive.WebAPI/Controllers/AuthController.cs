using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Commands;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }


        [HttpPost(Name = "signup")]
        public async Task<ActionResult<SignUpResponse>> SignUp([FromForm] SignUpRequest req)
        {
            await authService.SignUp(req.Email, req.Password);
            var resp = new SignUpResponse { };
            return Ok(resp);
        }

        [HttpPost(Name = "signin")]
        public async Task<ActionResult<SignInResponse>> SignIn([FromForm] SignInRequest req)
        {
            var accessToken = await authService.SignIn(req.Email, req.Password);
            var resp = new SignInResponse { 
                AccessToken = accessToken
            };
            return Ok(resp);
        }
    }
}
