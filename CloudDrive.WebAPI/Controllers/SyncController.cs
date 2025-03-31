using CloudDrive.Infrastructure.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [Route("sync")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        // Return the current server-side state of user's storage
        [HttpGet(Name = "Sync")]
        [Authorize]
        public async Task<ActionResult<SyncGetResponse>> Sync()
        {
            throw new NotImplementedException();
        }
    }
}
