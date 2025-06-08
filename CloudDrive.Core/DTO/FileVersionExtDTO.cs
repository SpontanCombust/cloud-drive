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
        public required bool IsDir { get; set; }
        public bool Deleted { get; set; }
        public DateTime FileCreatedDate { get; set; }
        public DateTime? FileModifiedDate { get; set; }

        public Guid FileVersionId { get; set; }
        public string? ClientDirPath { get; set; }
        public required string ClientFileName { get; set; }
        public int VersionNr { get; set; }
        public string? Md5 { get; set; }
        public long? SizeBytes { get; set; }
        public DateTime FileVersionCreatedDate { get; set; }
    }
}
