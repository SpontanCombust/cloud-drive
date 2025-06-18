namespace CloudDrive.App.Services
{
    public interface IUserSettingsService
    {
        Uri? ServerUrl { get; set; }
        string WatchedFolderPath { get; set; }

        bool SettingsWereSaved();
        Task SaveSettingsAsync();
        Task LoadSettingsAsync();
    }
}
