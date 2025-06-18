using CommunityToolkit.Mvvm.ComponentModel;

namespace CloudDrive.App.ViewModels
{
    public partial class FileVersionHistoryListItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private Guid fileVersionId;

        [ObservableProperty]
        private string fileName;

        [ObservableProperty]
        private DateTime createdDate;

        [ObservableProperty]
        private int versionNr;

        [ObservableProperty]
        private long sizeBytes;

        [ObservableProperty]
        private string md5Hash;


        public FileVersionHistoryListItemViewModel(
            Guid fileVersionId, 
            string fileName, 
            DateTime createdDate, 
            int versionNr, 
            long sizeBytes, 
            string md5Hash)
        {
            this.fileVersionId = fileVersionId;
            this.fileName = fileName;
            this.createdDate = createdDate;
            this.versionNr = versionNr;
            this.sizeBytes = sizeBytes;
            this.md5Hash = md5Hash;
        }
    }
}
