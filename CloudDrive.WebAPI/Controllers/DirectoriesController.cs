using CloudDrive.Core.DTO;
using CloudDrive.Core.Services;
using CloudDrive.WebAPI.Extensions;
using CloudDrive.WebAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [ApiController]
    [Route("files/dirs")]
    [Authorize]
    public class DirectoriesController : Controller
    {
        private readonly IFileManagerService fileManagerService;
        private readonly IFileInfoService fileInfoService;

        public DirectoriesController(
            IFileManagerService fileManagerService,
            IFileInfoService fileInfoService)
        {
            this.fileManagerService = fileManagerService;
            this.fileInfoService = fileInfoService;
        }


        // Upload new file to the server
        [HttpPost(Name = "CreateDirectory")]
        [ProducesResponseType(typeof(CreateDirectoryResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateDirectoryResponse>> Create([FromForm] CreateDirectoryRequest req)
        {
            Guid userId = User.GetId();

            try
            {
                var result = await fileManagerService.CreateDirectory(userId, req.ClientFileName, req.ClientDirPath);

                var response = new CreateDirectoryResponse
                {
                    FileInfo = result.FileInfo,
                    FirstFileVersionInfo = result.FirstFileVersionInfo,
                    ServerTime = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{fileId}", Name = "UpdateDirectory")]
        [ProducesResponseType(typeof(UpdateDirectoryResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UpdateDirectoryResponse>> Update([FromRoute] Guid fileId, [FromForm] UpdateDirectoryRequest req)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            try
            {
                var result = await fileManagerService.UpdateDirectory(fileId, req.ClientFileName, req.ClientDirPath);

                var resp = new UpdateDirectoryResponse
                {
                    NewFileVersionInfo = result.ActiveFileVersion,
                    NewSubfileVersionInfosExt = result.NewSubfileVersionsExt,
                    Changed = result.Changed,
                    ServerTime = DateTime.UtcNow
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{fileId}", Name = "DeleteDirectory")]
        [ProducesResponseType(typeof(DeleteDirectoryResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeleteDirectoryResponse>> Delete([FromRoute] Guid fileId)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            try
            {
                var result = await fileManagerService.DeleteDirectory(fileId);

                var resp = new DeleteDirectoryResponse
                {
                    AffectedSubfiles = result.AffectedSubfiles,
                    AffectedSubfileVersions = result.AffectedSubfileVersions,
                    ServerTime = DateTime.UtcNow
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{fileId}/restore", Name = "RestoreDirectory")]
        [ProducesResponseType(typeof(RestoreDirectoryResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<RestoreDirectoryResponse>> Restore([FromRoute] Guid fileId, [FromQuery] RestoreDirectoryRequestQuery req)
        {
            if (req.RestoreSubfiles)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, "Restoring subfiles is not supported");
            }

            Guid userId = User.GetId();

            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return Unauthorized("You do not have permission to modify this directory.");
            }

            try
            {
                RestoreDirectoryResultDTO restoration;
                if (req.FileVersionId != null)
                {
                    restoration = await fileManagerService.RestoreDirectory(fileId, req.FileVersionId.GetValueOrDefault());
                }
                else
                {
                    restoration = await fileManagerService.RestoreDirectory(fileId);
                }

                var resp = new RestoreDirectoryResponse
                {
                    FileInfo = restoration.FileInfo,
                    ActiveFileVersionInfo = restoration.ActiveFileVersionInfo,
                    ServerTime = DateTime.UtcNow
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
