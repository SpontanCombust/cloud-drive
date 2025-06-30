using CloudDrive.Core.DTO;
using CloudDrive.Core.Services;
using CloudDrive.WebAPI.Extensions;
using CloudDrive.WebAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [ApiController]
    [Route("file-info")]
    [Authorize]
    public class FileInfoController : Controller
    {
        private readonly IFileInfoService fileInfoService;

        public FileInfoController(IFileInfoService fileInfoService)
        {
            this.fileInfoService = fileInfoService;
        }


        [HttpGet("{fileId}", Name = "GetFileInfo")]
        [ProducesResponseType(typeof(FileDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<FileDTO>> GetForFile([FromRoute] Guid fileId)
        {
            Guid userId = User.GetId();

            try
            {
                var fileInfo = await fileInfoService.GetInfoForFile(fileId);
                if (fileInfo == null || fileInfo.UserId != userId)
                {
                    return NotFound();
                }

                return Ok(fileInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("last-update", Name = "GetLatestFileChangeDateTime")]
        [ProducesResponseType(typeof(DateTime), StatusCodes.Status200OK)]
        public async Task<ActionResult<DateTime>> GetLatestFileChangeDateTime()
        {
            Guid userId = User.GetId();

            var lastChange = await fileInfoService.LatestFileChangeDateTimeForUser(userId);
            return Ok(lastChange ?? DateTime.MinValue.ToUniversalTime());
        }
    }
}
