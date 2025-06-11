using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDrive.Core.DTO
{
    public class RestoreFileResultDTO
    {
        public required FileDTO FileInfo { get; set; }
        public required FileVersionDTO ActiveFileVersionInfo { get; set; }
    }
}
