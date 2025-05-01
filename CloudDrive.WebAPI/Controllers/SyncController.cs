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
        private readonly IFileInfoService fileInfoService;
        private readonly IFileVersionInfoService fileVersionInfoService;

        public SyncController(
            IFileInfoService fileInfoService,
            IFileVersionInfoService fileVersionInfoService)
        {
            this.fileInfoService = fileInfoService;
            this.fileVersionInfoService = fileVersionInfoService;
        }


        //TODO add query params to search through deleted as well
        /// <summary>
        /// Return the current server-side state of user's storage
        /// </summary>
        [HttpGet(Name = "SyncAll")]
        [Authorize]
        [ProducesResponseType(typeof(SyncAllResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SyncAllResponse>> SyncAll()
        {
            Guid userId = User.GetId();

            try
            {
                var infoDtos = await fileVersionInfoService.GetInfoForAllLatestUserFileVersions(userId);

                var resp = new SyncAllResponse { 
                    CurrentFileVersionsInfos = infoDtos 
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Return the current server-side state of a specific file
        /// </summary>
        [HttpGet("{fileId}", Name = "SyncFile")]
        [Authorize]
        [ProducesResponseType(typeof(SyncFileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SyncFileResponse>> SyncFile([FromRoute] Guid fileId)
        {
            Guid userId = User.GetId();

            var fileInfo = await fileInfoService.GetInfoForFile(fileId);
            if (fileInfo == null || fileInfo.UserId != userId)
            {
                return NotFound();
            }

            var fileVersionInfo = await fileVersionInfoService.GetInfoForLatestFileVersion(fileId);
            if (fileVersionInfo != null)
            {
                var resp = new SyncFileResponse 
                { 
                    FileInfo = fileInfo,
                    CurrentFileVersionInfo = fileVersionInfo 
                };
                return Ok(resp);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
