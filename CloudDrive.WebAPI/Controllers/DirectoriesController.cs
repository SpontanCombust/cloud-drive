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
                    FirstFileVersionInfo = result.FirstFileVersionInfo
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
                    NewFileVersionInfo = result
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{fileId}", Name = "DeleteDirectory")]
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
                await fileManagerService.DeleteDirectory(fileId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
