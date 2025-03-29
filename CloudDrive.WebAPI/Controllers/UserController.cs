using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.DTO;
using CloudDrive.Infrastructure.Repositories;
using CloudDrive.WebAPI.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloudDrive.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService userService;

        public UserController(IUserService userService)
        {
            this.userService = userService;
        }


        [HttpGet]
        public ActionResult<UserDTO> Get()
        {
            var userId = User.GetId();
            var user = userService.GetUserById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var dto = user.Adapt<UserDTO>();
            return Ok(dto);
        }
    }
}
