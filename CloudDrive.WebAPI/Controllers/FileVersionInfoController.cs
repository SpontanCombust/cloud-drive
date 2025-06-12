using CloudDrive.Core.DTO;
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
        public async Task<ActionResult<GetFileVersionInfosResponse>> GetForFile([FromRoute] Guid fileId)
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

        [HttpGet("{fileVersionId}", Name = "GetFileVersionInfo")]
        [ProducesResponseType(typeof(FileVersionDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<FileVersionDTO>> Get([FromRoute] Guid fileVersionId)
        {
            Guid userId = User.GetId();

            try
            {
                var fv = await fileVersionInfoService.GetInfoForUserFileVersion(userId, fileVersionId);
                if (fv == null)
                {
                    return NotFound();
                }

                return Ok(fv);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{fileVersionId}/ext", Name = "GetFileVersionInfoExt")]
        [ProducesResponseType(typeof(FileVersionExtDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<FileVersionExtDTO>> GetExt([FromRoute] Guid fileVersionId)
        {
            Guid userId = User.GetId();

            try
            {
                var fvext = await fileVersionInfoService.GetInfoForUserFileVersionExt(userId, fileVersionId);
                if (fvext == null)
                {
                    return NotFound();
                }

                return Ok(fvext);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
