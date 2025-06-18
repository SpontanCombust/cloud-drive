using CloudDrive.App.Services;
using Newtonsoft.Json;
using System.IO;

namespace CloudDrive.App.ServicesImpl
{
    internal class UserSettings
    {
        public Uri? ServerUrl { get; set; } = null;
        public string WatchedFolderPath { get; set; } = DefaultWatchedFolderPath;


        private static string DefaultWatchedFolderPath
        {
            get
            {
                var docsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CloudDrive");
                Directory.CreateDirectory(docsPath); // Ensure the directory exists
                return docsPath;
            }
        }
    }

    public class AppDataUserSettingsService : IUserSettingsService
    {
        private UserSettings _userSettings;
        
        public AppDataUserSettingsService()
        {
            _userSettings = new UserSettings();
        }


        public Uri? ServerUrl { 
            get => _userSettings.ServerUrl; 
            set => _userSettings.ServerUrl = value; 
        }
        public string WatchedFolderPath {
            get => _userSettings.WatchedFolderPath; 
            set => _userSettings.WatchedFolderPath = value; 
        }


        public bool SettingsWereSaved()
        {
            return File.Exists(SettingsFilePath);
        }

        public Task SaveSettingsAsync()
        {
            string json = JsonConvert.SerializeObject(_userSettings, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
            return Task.CompletedTask;
        }

        public Task LoadSettingsAsync()
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _userSettings = new UserSettings(); // Reset to default if file is empty
                }
                else
                {
                    var savedSettings = JsonConvert.DeserializeObject<UserSettings>(json);

                    if (savedSettings != null)
                    {
                        _userSettings = savedSettings;
                    }
                }
            }

            return Task.CompletedTask;
        }


        private string SettingsFilePath
        {
            get
            {
                var appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudDrive");
                Directory.CreateDirectory(appDataPath);
                return System.IO.Path.Combine(appDataPath, "settings.json");
            }
        }
    }    
}
