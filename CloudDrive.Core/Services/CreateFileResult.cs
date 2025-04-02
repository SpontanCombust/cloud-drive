using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Services
{
    public class CreateFileResult
    {
        public required Entities.File FileInfo { get; set; }
        public required Entities.FileVersion FirstFileVersionInfo { get; set; }
    }
}
