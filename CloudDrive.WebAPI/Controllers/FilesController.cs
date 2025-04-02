using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Commands;
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
    [ApiController]
    [Route("files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileManagerService fileManagerService;
        private readonly IFileInfoService fileInfoService;

        public FilesController(
            IFileManagerService fileManagerService,
            IFileInfoService fileInfoService)
        {
            this.fileManagerService = fileManagerService;
            this.fileInfoService = fileInfoService;
        }


        // Upload new file to the server
        [HttpPost(Name = "CreateFile")]
        public async Task<ActionResult<CreateFileResponse>> Create([FromForm] CreateFileRequest req)
        {
            Guid userId = User.GetId();
            Stream fileStream = req.File.OpenReadStream();
            string fileName = req.File.FileName;
            var result = await fileManagerService.CreateFile(userId, fileStream, fileName, req.ClientDirPath);

            var response = new CreateFileResponse {
                FileInfo = result.FileInfo.Adapt<FileDTO>(),
                FirstFileVersionInfo = result.FirstFileVersionInfo.Adapt<FileVersionDTO>()
            };

            return Ok(response);
        }

        // Get latest version of a given file from the server
        [HttpGet("{fileId}", Name = "GetLatestFileVersion")]
        //TODO use more of these attributes elsewhere
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestVersion(Guid fileId)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            var result = await fileManagerService.GetLatestFileVersion(fileId);
            if (result == null)
            {
                return NotFound();
            }

            return File(result.FileContent, "application/octet-stream", result.ClientFileName);
        }

        // Get a specific version of the file from the server
        [HttpGet("{fileId}/{versionNr}", Name = "GetFileVersion")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVersion(Guid fileId, int versionNr)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            var result = await fileManagerService.GetFileVersion(fileId, versionNr);
            if (result == null)
            {
                return NotFound();
            }

            return File(result.FileContent, "application/octet-stream", result.ClientFileName);
        }
    }
}
