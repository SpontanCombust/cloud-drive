using CloudDrive.Core.Services;
using CloudDrive.Core.Mappers;
using CloudDrive.Infrastructure.DTO;
using CloudDrive.Infrastructure.Repositories;
using CloudDrive.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
