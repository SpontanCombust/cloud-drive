using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Commands;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }


        [HttpPost("signup")]
        public async Task<ActionResult<SignUpResponse>> SignUp([FromForm] SignUpRequest req)
        {
            try
            {
                await authService.SignUp(req.Email, req.Password);
                var resp = new SignUpResponse { };
                return Ok(resp);
            } 
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("signin")]
        public async Task<ActionResult<SignInResponse>> SignIn([FromForm] SignInRequest req)
        {
            try
            {
                var accessToken = await authService.SignIn(req.Email, req.Password);
                var resp = new SignInResponse { 
                    AccessToken = accessToken
                };
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
