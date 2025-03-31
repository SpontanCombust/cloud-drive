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
        // Upload new file to the server
        [HttpPost(Name = "CreateFile")]
        public async Task<ActionResult<CreateFileResponse>> Create([FromForm] CreateFileRequest req)
        {
            throw new NotImplementedException();
        }

        // Get latest version of a given file from the server
        [HttpGet("{fileId}", Name = "GetLatestFileVersion")]
        [Produces("application/octet-stream")]
        public async Task<ActionResult<byte[]>> GetLatestVersion(Guid fileId)
        {
            throw new NotImplementedException();
        }

        // Get a specific version of the file from the server
        [HttpGet("{fileId}/{versionNr}", Name = "GetFileVersion")]
        [Produces("application/octet-stream")]
        public async Task<ActionResult<byte[]>> GetVersion(Guid fileId, int versionNr)
        {
            throw new NotImplementedException();
        }
    }
}
