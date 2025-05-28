using CloudDrive.Core.Services;
using CloudDrive.Core.Mappers;
using CloudDrive.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Controllers
{
    [Route("user")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService userService;

        public UserController(IUserService userService)
        {
            this.userService = userService;
        }


        [HttpGet(Name = "GetUser")]
        [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDTO>> Get()
        {
            var userId = User.GetId();
            var user = await userService.GetUserById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var dto = user.ToDto();
            return Ok(dto);
        }
    }
}
