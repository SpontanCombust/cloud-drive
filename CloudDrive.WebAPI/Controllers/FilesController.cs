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
            string fileName = req.File.Name;
            var result = await fileManagerService.CreateFile(userId, fileStream, fileName, req.ClientDirPath);

            var response = new CreateFileResponse {
                FileInfo = result.FileInfo.Adapt<FileDTO>(),
                FirstFileVersionInfo = result.FirstFileVersionInfo.Adapt<FileVersionDTO>()
            };

            return Ok(response);
        }

        // Get latest version of a given file from the server
        [HttpGet("{fileId}", Name = "GetLatestFileVersion")]
        [Produces("application/octet-stream")]
        public async Task<ActionResult<byte[]>> GetLatestVersion(Guid fileId)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            var bytes = await fileManagerService.GetLatestFileVersion(fileId);
            if (bytes == null)
            {
                return NotFound();
            }

            return Ok(bytes);
        }

        // Get a specific version of the file from the server
        [HttpGet("{fileId}/{versionNr}", Name = "GetFileVersion")]
        [Produces("application/octet-stream")]
        public async Task<ActionResult<byte[]>> GetVersion(Guid fileId, int versionNr)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            var bytes = await fileManagerService.GetFileVersion(fileId, versionNr);
            if (bytes == null)
            {
                return NotFound();
            }

            return Ok(bytes);
        }
    }
}
