using CloudDrive.App.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CloudDrive.App.ViewModels
{
    public partial class FileVersionListItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private Guid fileVersionId;

        [ObservableProperty]
        private string clientPath;

        [ObservableProperty]
        private int versionNr;

        [ObservableProperty]
        private string? md5;

        [ObservableProperty]
        private long? sizeBytes;

        [ObservableProperty]
        private DateTime createdDate;

        [ObservableProperty]
        private bool active;



        public FileVersionListItemViewModel(
            Guid fileVersionId, 
            string clientPath, 
            int versionNr, 
            string? md5, 
            long? sizeBytes, 
            DateTime createdDate, 
            bool active)
        {
            this.fileVersionId = fileVersionId;
            this.clientPath = clientPath;
            this.versionNr = versionNr;
            this.md5 = md5;
            this.sizeBytes = sizeBytes;
            this.createdDate = createdDate;
            this.active = active;
        }

        public FileVersionListItemViewModel(FileDTO fileInfo, FileVersionDTO fileVersionInfo)
        {
            this.fileVersionId = fileVersionInfo.FileVersionId;
            this.clientPath = fileVersionInfo.ClientFilePath();
            this.versionNr = fileVersionInfo.VersionNr;
            this.md5 = fileVersionInfo.Md5;
            this.sizeBytes = fileVersionInfo.SizeBytes;
            this.createdDate = fileVersionInfo.CreatedDate.DateTime;
            this.active = fileInfo.ActiveFileVersionId == fileVersionInfo.FileVersionId;
        }
    }
}
