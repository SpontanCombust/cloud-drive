using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDrive.App.Services
{
    public interface ISyncService
    {
        Task SynchronizeAllAsync();
        Task<FileVersionDTO> UploadFileAsync(string filePath);
        Task DownloadFileAsync(string fileId, string destinationPath);
    }
}
