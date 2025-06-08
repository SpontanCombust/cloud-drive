using CloudDrive.Core.Services;
using CloudDrive.WebAPI.Extensions;
using CloudDrive.WebAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [ApiController]
    [Route("file-info/{fileId}/versions")]
    [Authorize]
    public class FileVersionInfoController : Controller
    {
        private readonly IFileVersionInfoService fileVersionInfoService;

        public FileVersionInfoController(IFileVersionInfoService fileVersionInfoService)
        {
            this.fileVersionInfoService = fileVersionInfoService;
        }


        [HttpGet(Name = "GetFileVersionInfosForFile")]
        [ProducesResponseType(typeof(GetFileVersionInfosResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GetFileVersionInfosResponse>> Get([FromRoute] Guid fileId)
        {
            Guid userId = User.GetId();

            try
            {
                var fileVersions = await fileVersionInfoService.GetInfoForUserFileVersions(userId, fileId);

                var resp = new GetFileVersionInfosResponse
                {
                    FileVersionsInfos = fileVersions
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
