using CloudDrive.Core.Services;
using CloudDrive.WebAPI.Extensions;
using CloudDrive.WebAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [Route("sync")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly IFileVersionInfoService fileVersionInfoService;

        public SyncController(IFileVersionInfoService fileVersionInfoService)
        {
            this.fileVersionInfoService = fileVersionInfoService;
        }


        // Return the current server-side state of user's storage
        [HttpGet(Name = "Sync")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SyncGetResponse>> Sync()
        {
            Guid userId = User.GetId();

            try
            {
                var infoDtos = await fileVersionInfoService.GetInfoForAllLatestUserFileVersions(userId);

                var resp = new SyncGetResponse
                {
                    CurrentFileVersionsInfos = infoDtos
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }   
        }
    }
}
