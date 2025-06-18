using CommunityToolkit.Mvvm.ComponentModel;

namespace CloudDrive.App.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string serverUrl;

        [ObservableProperty]
        private string serverUrlError;

        [ObservableProperty]
        private string folderPath;

        [ObservableProperty]
        private string folderPathError;


        public SettingsPageViewModel()
        {
            serverUrl = string.Empty;
            serverUrlError = string.Empty;
            folderPath = string.Empty;
            folderPathError = string.Empty;
        }
    }
}
