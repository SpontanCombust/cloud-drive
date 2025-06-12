using CloudDrive.Core.DTO;
using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Services;
using CloudDrive.WebAPI.Extensions;
using CloudDrive.WebAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudDrive.WebAPI.Controllers
{
    [ApiController]
    [Route("files/regular")]
    [Authorize]
    public class RegularFilesController : ControllerBase
    {
        private readonly IFileManagerService fileManagerService;
        private readonly IFileInfoService fileInfoService;

        public RegularFilesController(
            IFileManagerService fileManagerService,
            IFileInfoService fileInfoService)
        {
            this.fileManagerService = fileManagerService;
            this.fileInfoService = fileInfoService;
        }


        // Upload new file to the server
        [HttpPost(Name = "CreateFile")]
        [ProducesResponseType(typeof(CreateFileResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateFileResponse>> Create([FromForm] CreateFileRequest req)
        {
            Guid userId = User.GetId();
            Stream fileStream = req.File.OpenReadStream();
            string fileName = req.File.FileName;

            try
            {
                var result = await fileManagerService.CreateFile(userId, fileStream, fileName, req.ClientDirPath);

                var response = new CreateFileResponse
                {
                    FileInfo = result.FileInfo,
                    FirstFileVersionInfo = result.FirstFileVersionInfo
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }            
        }

        // Get latest version of a given file from the server
        [HttpGet("{fileId}", Name = "GetActiveFileVersion")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActiveVersion(Guid fileId)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            try
            {
                var result = await fileManagerService.GetActiveFileVersion(fileId);
                if (result == null)
                {
                    return NotFound();
                }
                else if (result.FileContent == null)
                {
                    return NotFound();
                }

                return File(result.FileContent, "application/octet-stream", result.ClientFileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Get a specific version of the file from the server
        [HttpGet("{fileId}/{versionNr}", Name = "GetFileVersion")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVersion(Guid fileId, int versionNr)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            try
            {
                var result = await fileManagerService.GetFileVersion(fileId, versionNr);
                if (result == null)
                {
                    return NotFound();
                }
                else if (result.FileContent == null)
                {
                    return NotFound();
                }

                return File(result.FileContent, "application/octet-stream", result.ClientFileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{fileId}", Name = "UpdateFile")]
        [ProducesResponseType(typeof(UpdateFileResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UpdateFileResponse>> Update([FromRoute] Guid fileId, [FromForm] UpdateFileRequest req)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            try
            {
                Stream fileStream = req.File.OpenReadStream();
                string fileName = req.File.FileName;
                var result = await fileManagerService.UpdateFile(fileId, fileStream, fileName, req.ClientDirPath);

                var resp = new UpdateFileResponse
                {
                    NewFileVersionInfo = result.ActiveFileVersion,
                    Changed = result.Changed,
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{fileId}", Name = "DeleteFile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid fileId)
        {
            Guid userId = User.GetId();
            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return NotFound();
            }

            try
            {
                await fileManagerService.DeleteFile(fileId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{fileId}/restore", Name = "RestoreFile")]
        [ProducesResponseType(typeof(RestoreFileResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<RestoreFileResponse>> Restore([FromRoute] Guid fileId, [FromQuery] RestoreFileRequestQuery q)
        {
            Guid userId = User.GetId();

            if (!await fileInfoService.FileBelongsToUser(fileId, userId))
            {
                return Unauthorized("You do not have permission to modify this file.");
            }

            try
            {
                RestoreFileResultDTO restoration;
                if (q.FileVersionId != null)
                {
                    restoration = await fileManagerService.RestoreFile(fileId, q.FileVersionId.GetValueOrDefault());
                }
                else
                {
                    restoration = await fileManagerService.RestoreFile(fileId);
                }

                var resp = new RestoreFileResponse
                {
                    FileInfo = restoration.FileInfo,
                    ActiveFileVersionInfo = restoration.ActiveFileVersionInfo
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
