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
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public bool IsDir { get; set; }
        public bool Deleted { get; set; }
        public DateTime FileCreatedDate { get; set; }
        public DateTime? FileModifiedDate { get; set; }

        public Guid FileVersionId { get; set; }
        public string? ClientDirPath { get; set; }
        public string ClientFileName { get; set; }
        public int VersionNr { get; set; }
        public string? Md5 { get; set; }
        public long? SizeBytes { get; set; }
        public DateTime FileVersionCreatedDate { get; set; }


        public FileVersionExtDTO(FileDTO f, FileVersionDTO fv)
        {
            FileId = f.FileId;
            UserId = f.UserId;
            IsDir = f.IsDir;
            Deleted = f.Deleted;
            FileCreatedDate = f.CreatedDate;
            FileModifiedDate = f.ModifiedDate;
            FileVersionId = fv.FileVersionId;
            ClientDirPath = fv.ClientDirPath;
            ClientFileName = fv.ClientFileName;
            VersionNr = fv.VersionNr;
            Md5 = fv.Md5;
            SizeBytes = fv.SizeBytes;
            FileVersionCreatedDate = fv.CreatedDate;
        }


        /// <summary>
        /// Relative path to this file or directory on the client side
        /// </summary>
        public string ClientFilePath()
        {
            return Path.Combine(
                ClientDirPath ?? string.Empty,
                ClientFileName
            );
        }
    }
}
