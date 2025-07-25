﻿using CloudDrive.Core.DTO;

namespace CloudDrive.WebAPI.Model
{
    public class SyncAllExtResponse
    {
        public required FileVersionExtDTO[] CurrentFileVersionsInfosExt { get; set; }
        public required DateTime ServerTime { get; set; }
    }
}
