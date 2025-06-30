using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CloudDrive.Core.DTO
{
    /// <summary>
    /// DTO type combining attributes from FileVersionDTO and FileDTO
    /// </summary>
    public class FileVersionExtDTO
    {
        public FileDTO File { get; set; }
        public FileVersionDTO FileVersion { get; set; }


        public FileVersionExtDTO(FileDTO file, FileVersionDTO fileVersion)
        {
            File = file;
            FileVersion = fileVersion;
        }


        /// <summary>
        /// Relative path to this file or directory on the client side
        /// </summary>
        public string ClientFilePath()
        {
            return FileVersion.ClientFilePath();
        }

        public FileVersionDTO TrimExt()
        {
            return FileVersion;
        }
    }
}
