using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDrive.Core.Services
{
    public class GetFileResult
    {
        public byte[] FileContent { get; set; }
        public string ClientFileName { get; set; }
        public string ClientDirPath { get; set; }
    }
}
