using CloudDrive.Core.Services;
using CloudDrive.WebAPI.Model;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }


        [HttpPost("signup", Name = "SignUp")]
        [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        [HttpPost("signin", Name = "SignIn")]
        [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
